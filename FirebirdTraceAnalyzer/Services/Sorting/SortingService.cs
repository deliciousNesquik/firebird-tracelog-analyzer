using System.Reflection;
using FirebirdTraceParser.Attributes;
using FirebirdTraceParser.Models.Events;
using NLog;

namespace FirebirdTraceAnalyzer.Services.Sorting;

public sealed class SortingService : ISortingService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<Type, List<SortFieldInfo>> _fieldCache = new();
    private readonly Dictionary<string, SortDescriptor> _customSorts = new();

    // Кэш последних сортировок
    private List<SortDescriptor>? _lastGeneratedSorts;
    private HashSet<Type>? _lastEventTypes;

    public void RegisterCustomSort(SortDescriptor descriptor)
    {
        _customSorts[descriptor.Id] = descriptor;
        Logger.Info("Registered sort: {DisplayName}", descriptor.DisplayName);
    }

    public IReadOnlyList<SortDescriptor> GetAvailableSorts(IEnumerable<EventBase> events)
    {
        var eventList = events.ToList();

        if (eventList.Count == 0)
        {
            return _customSorts.Values
                .OrderBy(s => s.Priority)
                .ToList();
        }

        // Проверяем, изменились ли типы событий
        var currentEventTypes = eventList
            .Select(e => e.GetType())
            .ToHashSet();

        if (_lastEventTypes != null &&
            _lastGeneratedSorts != null &&
            currentEventTypes.SetEquals(_lastEventTypes))
        {
            Logger.Debug("Event types haven't changed, we'll reuse sorting");
            return _lastGeneratedSorts;
        }

        Logger.Info("Event types have changed, we are generating new sortings");

        var availableSorts = new List<SortDescriptor>(_customSorts.Values);

        // Собираем только ОБЩИЕ поля для пересеченных полей
        var commonFields = AnalyzeCommonFields(eventList);

        foreach (var field in commonFields)
        {
            var sortId = $"field_{field.PropertyPath.Replace(".", "_").ToLower()}";

            if (_customSorts.ContainsKey(sortId))
                continue;

            var descriptor = CreateFieldSort(field);
            availableSorts.Add(descriptor);
            _customSorts.Add(sortId, descriptor);
        }

        var result = availableSorts
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Priority)
            .ToList();

        _lastGeneratedSorts = result;
        _lastEventTypes = currentEventTypes;

        return result;
    }

    /// <summary>
    /// Собирает ОБЩИЕ поля (пересечение всех типов)
    /// </summary>
    private List<SortFieldInfo> AnalyzeCommonFields(List<EventBase> events)
    {
        var eventTypes = events
            .Select(e => e.GetType())
            .Distinct()
            .ToList();

        if (eventTypes.Count == 0)
            return [];

        var fieldsByType = eventTypes
            .Select(GetSortableFields)
            .ToList();

        if (fieldsByType.Count == 0)
            return [];

        // Находим пересечение
        var commonPaths = fieldsByType
            .Skip(1)
            .Aggregate(
                new HashSet<string>(fieldsByType[0].Select(f => f.PropertyPath)),
                (common, typeFields) =>
                {
                    common.IntersectWith(typeFields.Select(f => f.PropertyPath));
                    return common;
                });

        var commonFields = fieldsByType[0]
            .Where(f => commonPaths.Contains(f.PropertyPath))
            .OrderBy(f => f.Priority)
            .ToList();

        Logger.Info("Find {Count} common field(s) for filtering from {TypeCount} type(s)",
            commonFields.Count, eventTypes.Count);

        return commonFields;
    }

    private List<SortFieldInfo> GetSortableFields(Type eventType)
    {
        if (_fieldCache.TryGetValue(eventType, out var cached))
            return cached;

        var fields = new List<SortFieldInfo>();
        ScanProperties(eventType, string.Empty, fields, depth: 0);

        _fieldCache[eventType] = fields;
        return fields;
    }

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

            if (attr != null)
            {
                results.Add(new SortFieldInfo(
                    path,
                    attr.DisplayName,
                    prop.PropertyType,
                    attr.Category,
                    attr.Priority));
            }

            if (ShouldScanNestedType(prop.PropertyType))
                ScanProperties(prop.PropertyType, path, results, depth + 1);
        }
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
               type.Namespace?.StartsWith("FirebirdTraceParser") == true;
    }

    private SortDescriptor CreateFieldSort(SortFieldInfo field)
    {
        return new SortDescriptor(
            $"field_{field.PropertyPath.Replace(".", "_").ToLower()}",
            field.DisplayName,
            CreatePropertyComparer(field.PropertyPath),
            field.IsDefault,
            field.Category,
            field.Priority + 100);
    }

    private Func<EventBase, EventBase, bool, int> CreatePropertyComparer(string propertyPath)
    {
        return (a, b, descending) =>
        {
            var valueA = GetPropertyValue(a, propertyPath);
            var valueB = GetPropertyValue(b, propertyPath);

            if (valueA == null && valueB == null) return 0;
            if (valueA == null) return 1;
            if (valueB == null) return -1;

            int result;

            if (valueA is IComparable comparableA)
                result = comparableA.CompareTo(valueB);
            else
                result = string.Compare(valueA.ToString(), valueB.ToString(), StringComparison.Ordinal);

            return descending ? -result : result;
        };
    }

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
            Logger.Warn("Sort is not found: {SortId}", sortId);
            return events;
        }

        var sorted = events.ToList();
        sorted.Sort((a, b) => descriptor.Comparer(a, b, descending));

        Logger.Info("Sorting applied: {DisplayName}, descending={Descending}",
            descriptor.DisplayName, descending);

        return sorted;
    }
}