using System.Reflection;
using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Models.Events;
using NLog;

namespace FirebirdTraceViewer.Services.Sorting;

public sealed class SortingService : ISortingService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Кэш метаданных полей по типу события
    private readonly Dictionary<Type, List<SortFieldInfo>> _fieldCache = new();

    // Зарегистрированные пользовательские сортировки
    private readonly Dictionary<string, SortDescriptor> _customSorts = new();
    
    private readonly Dictionary<string, SortDescriptor> _allSorts = new();

    public void RegisterCustomSort(SortDescriptor descriptor)
    {
        _customSorts[descriptor.Id] = descriptor;
        Logger.Info("Зарегистрирована сортировка: {DisplayName}", descriptor.DisplayName);
    }

    public IReadOnlyList<SortDescriptor> GetAvailableSorts(IEnumerable<EventBase> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0)
            // Только пользовательские сортировки
            return _customSorts.Values
                .OrderBy(s => s.Priority)
                .ToList();

        var availableSorts = new List<SortDescriptor>(_customSorts.Values);

        // 1. Только общие поля (безопасно):
        // var commonFields = AnalyzeCommonFields(eventList);
    
        // 2. Все уникальные поля (удобнее):
        var commonFields = AnalyzeAllFields(eventList);

        foreach (var field in commonFields)
        {
            var sortId = $"field_{field.PropertyPath.Replace(".", "_").ToLower()}";

            if (_customSorts.ContainsKey(sortId))
                continue; // Уже есть пользовательская сортировка

            var descriptor = CreateFieldSort(field);
            availableSorts.Add(descriptor);
            _customSorts.Add(sortId, descriptor);
        }

        return availableSorts
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Priority)
            .ToList();
    }

    /// <summary>
    ///     Анализирует коллекцию и находит общие поля для сортировки.
    /// </summary>
    private List<SortFieldInfo> AnalyzeCommonFields(List<EventBase> events)
    {
        var eventTypes = events
            .Select(e => e.GetType())
            .Distinct()
            .ToList();

        // Получаем поля для каждого типа
        var fieldsByType = eventTypes
            .Select(type => GetSortableFields(type))
            .ToList();

        if (fieldsByType.Count == 0)
            return new List<SortFieldInfo>();

        // Находим общие поля (пересечение по PropertyPath)
        var commonFields = fieldsByType
            .Skip(1)
            .Aggregate(
                new HashSet<string>(fieldsByType[0].Select(f => f.PropertyPath)),
                (commonPaths, typeFields) =>
                {
                    commonPaths.IntersectWith(typeFields.Select(f => f.PropertyPath));
                    return commonPaths;
                });

        // Возвращаем метаданные общих полей
        return fieldsByType[0]
            .Where(f => commonFields.Contains(f.PropertyPath))
            .OrderBy(f => f.Priority)
            .ToList();
    }
    
    /// <summary>
    /// Собирает все уникальные поля из всех типов событий.
    /// </summary>
    private List<SortFieldInfo> AnalyzeAllFields(List<EventBase> events)
    {
        var eventTypes = events
            .Select(e => e.GetType())
            .Distinct()
            .ToList();

        if (eventTypes.Count == 0)
            return new List<SortFieldInfo>();

        // Получаем поля для каждого типа
        var allFields = eventTypes
            .SelectMany(type => GetSortableFields(type))
            .GroupBy(f => f.PropertyPath) // Группируем по пути
            .Select(g => g.First()) // Берём первую метаданную
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Priority)
            .ToList();

        Logger.Info("Собрано уникальных полей: {Count} из {TypeCount} типов", 
            allFields.Count, eventTypes.Count);

        return allFields;
    }

    /// <summary>
    ///     Получает сортируемые поля для типа события (с кэшированием).
    /// </summary>
    private List<SortFieldInfo> GetSortableFields(Type eventType)
    {
        if (_fieldCache.TryGetValue(eventType, out var cached))
        {
            Logger.Debug("Использую кэш для типа: {Type}, найдено полей: {Count}", 
                eventType.Name, cached.Count);
            return cached;
        }

        var fields = new List<SortFieldInfo>();

        Logger.Info("Сканирую тип события: {Type}", eventType.FullName);
    
        // Сканируем свойства самого события
        ScanProperties(eventType, string.Empty, fields, depth: 0);

        Logger.Info("Для типа {Type} найдено {Count} сортируемых полей: {Fields}", 
            eventType.Name, 
            fields.Count,
            string.Join(", ", fields.Select(f => f.PropertyPath)));

        _fieldCache[eventType] = fields;
        return fields;
    }

    /// <summary>
    ///     Рекурсивно сканирует свойства типа.
    /// </summary>
    private void ScanProperties(Type type, string pathPrefix, List<SortFieldInfo> results, int depth = 0)
    {
        if (depth > 3) return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<SortableFieldAttribute>();

            var path = string.IsNullOrEmpty(pathPrefix)
                ? prop.Name
                : $"{pathPrefix}.{prop.Name}";

            // Если свойство помечено атрибутом - добавляем его
            if (attr != null)
            {
                results.Add(new SortFieldInfo(
                    path,
                    attr.DisplayName,
                    prop.PropertyType,
                    attr.Category,
                    attr.Priority));

                Logger.Debug("Найдено сортируемое поле: {Path} ({DisplayName})", path, attr.DisplayName);
            }

            // Рекурсивно сканируем вложенные объекты
            if (ShouldScanNestedType(prop.PropertyType))
            {
                Logger.Debug("Сканирую вложенный тип: {Type} для свойства {PropName}",
                    prop.PropertyType.Name, prop.Name);

                ScanProperties(prop.PropertyType, path, results, depth + 1);
            }
        }
    }

    /// <summary>
    ///     Определяет, нужно ли сканировать тип на вложенные свойства.
    /// </summary>
    private static bool ShouldScanNestedType(Type type)
    {
        // Пропускаем примитивы, строки, перечисления
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return false;

        // Пропускаем коллекции
        if (type.IsGenericType)
            return false;

        // Пропускаем системные типы
        if (type.Namespace?.StartsWith("System") == true)
            return false;

        // Сканируем только record/class из наших проектов
        return type.IsClass &&
               type.Namespace?.StartsWith("FirebirdTraceParser.Core") == true;
    }

    /// <summary>
    ///     Создаёт дескриптор сортировки для поля.
    /// </summary>
    private SortDescriptor CreateFieldSort(SortFieldInfo field)
    {
        return new SortDescriptor(
            $"field_{field.PropertyPath.Replace(".", "_").ToLower()}",
            field.DisplayName,
            CreatePropertyComparer(field.PropertyPath),
            field.Category,
            field.Priority + 100); // Смещаем приоритет ниже встроенных
    }

    /// <summary>
    ///     Создаёт функцию сравнения по пути к свойству.
    /// </summary>
    private Comparison<EventBase> CreatePropertyComparer(string propertyPath)
    {
        return (a, b) =>
        {
            var valueA = GetPropertyValue(a, propertyPath);
            var valueB = GetPropertyValue(b, propertyPath);

            // null всегда в конце
            if (valueA == null && valueB == null) return 0;
            if (valueA == null) return 1;
            if (valueB == null) return -1;

            // Сравниваем через IComparable
            if (valueA is IComparable comparableA)
                return comparableA.CompareTo(valueB);

            return string.Compare(valueA.ToString(), valueB.ToString(), StringComparison.Ordinal);
        };
    }

    /// <summary>
    ///     Получает значение свойства по пути (поддерживает вложенность).
    /// </summary>
    private object? GetPropertyValue(object obj, string propertyPath)
    {
        var parts = propertyPath.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current == null) return null;

            var prop = current.GetType().GetProperty(part);
            if (prop == null) return null;

            current = prop.GetValue(current);
        }

        return current;
    }

    public IEnumerable<EventBase> ApplySort(
        IEnumerable<EventBase> events,
        string sortId,
        bool descending = false)
    {
        if (!_customSorts.TryGetValue(sortId, out var descriptor))
        {
            Logger.Warn("Сортировка не найдена: {SortId}", sortId);
            return events;
        }

        var sorted = events.ToList();
        sorted.Sort(descriptor.Comparer);

        if (descending)
            sorted.Reverse();

        Logger.Info("Применена сортировка: {DisplayName}, descending={Descending}",
            descriptor.DisplayName, descending);

        return sorted;
    }
}