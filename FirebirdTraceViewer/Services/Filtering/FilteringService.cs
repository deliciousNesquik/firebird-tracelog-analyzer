using System.Collections.ObjectModel;
using System.Reflection;
using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Services.Filtering;
using NLog;

namespace FirebirdTraceViewer.Services.Filtering;

public interface IFilteringService
{
    /// <summary>
    /// Получает все доступные фильтры для коллекции событий.
    /// </summary>
    IReadOnlyList<FilterDescriptor> GetAvailableFilters(IEnumerable<EventBase> events);
    
    /// <summary>
    /// Применяет все активные фильтры к коллекции.
    /// </summary>
    IEnumerable<EventBase> ApplyFilters(IEnumerable<EventBase> events, IEnumerable<FilterDescriptor> filters);
    
    /// <summary>
    /// Регистрирует пользовательский фильтр.
    /// </summary>
    void RegisterCustomFilter(FilterDescriptor descriptor);
}

public sealed class FilteringService : IFilteringService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Кэш метаданных полей по типу события
    private readonly Dictionary<Type, List<FilterFieldInfo>> _fieldCache = new();

    // Пользовательские фильтры
    private readonly Dictionary<string, FilterDescriptor> _customFilters = new();

    public void RegisterCustomFilter(FilterDescriptor descriptor)
    {
        _customFilters[descriptor.Id] = descriptor;
        Logger.Info("Зарегистрирован фильтр: {DisplayName}", descriptor.DisplayName);
    }

    public IReadOnlyList<FilterDescriptor> GetAvailableFilters(IEnumerable<EventBase> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0)
            return _customFilters.Values
                .OrderBy(f => f.Priority)
                .ToList();

        var availableFilters = new List<FilterDescriptor>(_customFilters.Values);

        // Собираем все уникальные поля
        var allFields = AnalyzeAllFields(eventList);

        foreach (var field in allFields)
        {
            var filterId = $"filter_{field.PropertyPath.Replace(".", "_").ToLower()}";

            if (_customFilters.ContainsKey(filterId))
                continue;

            var descriptor = CreateFieldFilter(field, eventList);
            if (descriptor != null)
            {
                availableFilters.Add(descriptor);
                _customFilters[filterId] = descriptor;
            }
        }

        return availableFilters
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Priority)
            .ToList();
    }

    public IEnumerable<EventBase> ApplyFilters(
        IEnumerable<EventBase> events,
        IEnumerable<FilterDescriptor> filters)
    {
        var activeFilters = filters.Where(f => f.IsActive).ToList();

        if (activeFilters.Count == 0)
            return events;

        return events.Where(evt => activeFilters.All(filter => filter.FilterPredicate(evt)));
    }

    /// <summary>
    /// Собирает все уникальные фильтруемые поля из всех типов событий.
    /// </summary>
    private List<FilterFieldInfo> AnalyzeAllFields(List<EventBase> events)
    {
        var eventTypes = events
            .Select(e => e.GetType())
            .Distinct()
            .ToList();

        if (eventTypes.Count == 0)
            return new List<FilterFieldInfo>();

        var allFields = eventTypes
            .SelectMany(type => GetFilterableFields(type))
            .GroupBy(f => f.PropertyPath)
            .Select(g => g.First())
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Priority)
            .ToList();

        Logger.Info("Собрано уникальных фильтруемых полей: {Count} из {TypeCount} типов",
            allFields.Count, eventTypes.Count);

        return allFields;
    }

    /// <summary>
    /// Получает фильтруемые поля для типа события (с кэшированием).
    /// </summary>
    private List<FilterFieldInfo> GetFilterableFields(Type eventType)
    {
        if (_fieldCache.TryGetValue(eventType, out var cached))
            return cached;

        var fields = new List<FilterFieldInfo>();
        ScanProperties(eventType, string.Empty, fields, depth: 0);

        Logger.Info("Для типа {Type} найдено {Count} фильтруемых полей",
            eventType.Name, fields.Count);

        _fieldCache[eventType] = fields;
        return fields;
    }

    /// <summary>
    /// Рекурсивно сканирует свойства типа.
    /// </summary>
    private void ScanProperties(Type type, string pathPrefix, List<FilterFieldInfo> results, int depth = 0)
    {
        if (depth > 3) return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Проверяем FilterableField
            var filterAttr = prop.GetCustomAttribute<FilterableFieldAttribute>();
            
            if (filterAttr == null)
            {
                // Сканируем вложенные типы
                if (ShouldScanNestedType(prop.PropertyType))
                {
                    var nestedPath = string.IsNullOrEmpty(pathPrefix)
                        ? prop.Name
                        : $"{pathPrefix}.{prop.Name}";
                    
                    ScanProperties(prop.PropertyType, nestedPath, results, depth + 1);
                }
                continue;
            }

            var path = string.IsNullOrEmpty(pathPrefix)
                ? prop.Name
                : $"{pathPrefix}.{prop.Name}";

            var displayName = filterAttr?.DisplayName ?? prop.Name;
            var category = filterAttr?.Category ?? "Общие";
            var priority = filterAttr?.Priority ?? 100;
            var filterType = filterAttr?.FilterType ?? DetermineFilterType(prop.PropertyType);

            results.Add(new FilterFieldInfo(
                path,
                displayName,
                prop.PropertyType,
                category,
                priority,
                filterType));

            Logger.Debug("Найдено фильтруемое поле: {Path} ({DisplayName})", path, displayName);
        }
    }

    /// <summary>
    /// Автоматически определяет тип фильтра по типу свойства.
    /// </summary>
    private FilterType DetermineFilterType(Type propertyType)
    {
        // Убираем Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType.IsEnum)
            return FilterType.EnumMultiSelect;

        if (underlyingType == typeof(string))
            return FilterType.StringMultiSelect;

        if (underlyingType == typeof(DateTime))
            return FilterType.DateTimeRange;

        if (underlyingType == typeof(bool))
            return FilterType.Boolean;

        if (IsNumericType(underlyingType))
            return FilterType.NumericRange;

        return FilterType.TextSearch;
    }
    

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) ||
               type == typeof(decimal) || type == typeof(double) ||
               type == typeof(float) || type == typeof(short) ||
               type == typeof(byte);
    }

    private static bool ShouldScanNestedType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return false;

        if (type.IsGenericType)
            return false;

        if (type.Namespace?.StartsWith("System") == true)
            return false;

        return type.IsClass &&
               type.Namespace?.StartsWith("FirebirdTraceParser.Core") == true;
    }

    /// <summary>
    /// Создаёт дескриптор фильтра для поля.
    /// </summary>
    private FilterDescriptor? CreateFieldFilter(FilterFieldInfo field, List<EventBase> events)
    {
        var filterId = $"filter_{field.PropertyPath.Replace(".", "_").ToLower()}";

        return field.FilterType switch
        {
            FilterType.EnumMultiSelect => CreateEnumFilter(filterId, field, events),
            FilterType.StringMultiSelect => CreateStringFilter(filterId, field, events),
            FilterType.NumericRange => CreateNumericRangeFilter(filterId, field, events),
            FilterType.DateTimeRange => CreateDateTimeRangeFilter(filterId, field, events),
            _ => null
        };
    }

    #region Создание конкретных типов фильтров

    private FilterDescriptor CreateEnumFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        // Создаём коллекцию значений заранее
        var availableValues = new ObservableCollection<FilterValueItem>();

        // Собираем все уникальные значения enum
        var valueCounts = new Dictionary<object, int>();

        foreach (var evt in events)
        {
            var value = GetPropertyValue(evt, field.PropertyPath);
            if (value != null)
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        foreach (var (value, count) in valueCounts.OrderBy(kv => kv.Key.ToString()))
        {
            var displayName = GetEnumDisplayName(value);
            availableValues.Add(new FilterValueItem(value, displayName, count));
        }

        // Теперь создаём descriptor с уже заполненной коллекцией
        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.EnumMultiSelect,
            field.PropertyPath,
            evt => CheckEnumFilter(evt, field.PropertyPath, availableValues),
            field.Category,
            field.Priority);

        // Переносим значения в коллекцию дескриптора
        foreach (var item in availableValues)
            descriptor.AvailableValues.Add(item);

        return descriptor;
    }

    private FilterDescriptor CreateStringFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        // Создаём коллекцию значений заранее
        var availableValues = new ObservableCollection<FilterValueItem>();

        // Собираем все уникальные строки
        var valueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var evt in events)
        {
            var value = GetPropertyValue(evt, field.PropertyPath)?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        // Ограничиваем количество (топ-100)
        foreach (var (value, count) in valueCounts.OrderByDescending(kv => kv.Value).Take(100))
        {
            availableValues.Add(new FilterValueItem(value, value, count));
        }

        // Создаём descriptor
        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.StringMultiSelect,
            field.PropertyPath,
            evt => CheckStringFilter(evt, field.PropertyPath, availableValues),
            field.Category,
            field.Priority);

        // Переносим значения
        foreach (var item in availableValues)
            descriptor.AvailableValues.Add(item);

        return descriptor;
    }

    private FilterDescriptor CreateNumericRangeFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        var values = events
            .Select(evt => GetPropertyValue(evt, field.PropertyPath))
            .Where(v => v != null)
            .Cast<IComparable>()
            .ToList();

        if (values.Count == 0)
            return null!;

        var min = values.Min();
        var max = values.Max();

        var propertyPath = field.PropertyPath;

        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.NumericRange,
            propertyPath,
            evt => true,
            field.Category,
            field.Priority)
        {
            MinValue = min,
            MaxValue = max,
            CurrentMinValue = min,
            CurrentMaxValue = max
        };

        // Динамический предикат
        descriptor.UpdatePredicate(evt =>
        {
            var value = GetPropertyValue(evt, propertyPath) as IComparable;
            if (value == null)
                return false;

            var currentMin = descriptor.CurrentMinValue as IComparable;
            var currentMax = descriptor.CurrentMaxValue as IComparable;

            if (currentMin != null && value.CompareTo(currentMin) < 0)
                return false;

            if (currentMax != null && value.CompareTo(currentMax) > 0)
                return false;

            return true;
        });

        return descriptor;
    }

    private FilterDescriptor CreateDateTimeRangeFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        var values = events
            .Select(evt => GetPropertyValue(evt, field.PropertyPath))
            .Where(v => v != null)
            .Cast<DateTime>()
            .ToList();

        if (values.Count == 0)
            return null!;

        var min = values.Min();
        var max = values.Max();

        var propertyPath = field.PropertyPath;

        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.DateTimeRange,
            propertyPath,
            evt => true, // ← Временный предикат, обновим ниже
            field.Category,
            field.Priority)
        {
            MinValue = min,
            MaxValue = max,
            CurrentMinValue = min,
            CurrentMaxValue = max
        };

        // создаём ДИНАМИЧЕСКИЙ предикат, который читает CurrentMinValue/CurrentMaxValue
        descriptor.UpdatePredicate(evt =>
        {
            var value = GetPropertyValue(evt, propertyPath);
            if (value is not DateTime dateTime)
                return false;

            // Читаем ТЕКУЩИЕ значения из descriptor (не из closure!)
            var currentMin = descriptor.CurrentMinValue as DateTime?;
            var currentMax = descriptor.CurrentMaxValue as DateTime?;

            if (currentMin.HasValue && dateTime < currentMin.Value)
                return false;

            if (currentMax.HasValue && dateTime > currentMax.Value)
                return false;

            return true;
        });

        return descriptor;
    }

    #endregion

    #region Проверка фильтров

    private bool CheckEnumFilter(EventBase evt, string propertyPath, ObservableCollection<FilterValueItem> availableValues)
    {
        var selectedValues = availableValues
            .Where(v => v.IsSelected)
            .Select(v => v.Value)
            .ToHashSet();

        if (selectedValues.Count == 0)
            return true; // Фильтр не активен

        var value = GetPropertyValue(evt, propertyPath);
        return value != null && selectedValues.Contains(value);
    }

    private bool CheckStringFilter(EventBase evt, string propertyPath, ObservableCollection<FilterValueItem> availableValues)
    {
        var selectedValues = availableValues
            .Where(v => v.IsSelected)
            .Select(v => v.Value.ToString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedValues.Count == 0)
            return true;

        var value = GetPropertyValue(evt, propertyPath)?.ToString();
        return value != null && selectedValues.Contains(value);
    }

    private bool CheckNumericRangeFilter(EventBase evt, string propertyPath, object? minValue, object? maxValue)
    {
        var value = GetPropertyValue(evt, propertyPath) as IComparable;
        if (value == null) return false;

        if (minValue != null && value.CompareTo(minValue) < 0)
            return false;

        if (maxValue != null && value.CompareTo(maxValue) > 0)
            return false;

        return true;
    }

    private bool CheckDateTimeRangeFilter(EventBase evt, string propertyPath, object? minValue, object? maxValue)
    {
        var value = GetPropertyValue(evt, propertyPath);
        if (value is not DateTime dateTime) return false;

        if (minValue is DateTime min && dateTime < min)
            return false;

        if (maxValue is DateTime max && dateTime > max)
            return false;

        return true;
    }

    #endregion

    /// <summary>
    /// Получает значение свойства по пути (поддерживает вложенность).
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

    /// <summary>
    /// Получает отображаемое имя для enum (из Description или ToString).
    /// </summary>
    private string GetEnumDisplayName(object enumValue)
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString()!).FirstOrDefault();
        
        if (memberInfo != null)
        {
            var descAttr = memberInfo.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descAttr != null)
                return descAttr.Description;
        }

        return enumValue.ToString()!;
    }
}