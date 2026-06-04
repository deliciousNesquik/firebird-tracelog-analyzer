using System.Collections.ObjectModel;
using System.Reflection;
using FirebirdTraceAnalyzer.Services.EventProperties;
using FirebirdTraceParser.Attributes;
using FirebirdTraceParser.Models.Events;
using NLog;

namespace FirebirdTraceAnalyzer.Services.Filtering;

public sealed class FilteringService : IFilteringService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IEventPropertyAccessor _propertyAccessor;

    // Кэш метаданных полей по типу события
    private readonly Dictionary<Type, List<FilterFieldInfo>> _fieldCache = new();

    // Пользовательские фильтры
    private readonly Dictionary<string, FilterDescriptor> _customFilters = new();

    // Кэш последних созданных фильтров (для оптимизации)
    private List<FilterDescriptor>? _lastGeneratedFilters;
    private HashSet<Type>? _lastEventTypes;

    public FilteringService(IEventPropertyAccessor propertyAccessor)
    {
        _propertyAccessor = propertyAccessor ?? throw new ArgumentNullException(nameof(propertyAccessor));
    }

    public void RegisterCustomFilter(FilterDescriptor descriptor)
    {
        _customFilters[descriptor.Id] = descriptor;
        Logger.Info("Register filter: {DisplayName}", descriptor.DisplayName);
    }

    public IReadOnlyList<FilterDescriptor> GetAvailableFilters(IEnumerable<EventBase> events)
    {
        var eventList = events.ToList();
        
        if (eventList.Count == 0)
        {
            return _customFilters.Values
                .OrderBy(f => f.Priority)
                .ToList();
        }

        // Проверяем, изменились ли типы событий
        var currentEventTypes = eventList
            .Select(e => e.GetType())
            .ToHashSet();

        if (_lastEventTypes != null && 
            _lastGeneratedFilters != null && 
            currentEventTypes.SetEquals(_lastEventTypes))
        {
            // Типы не изменились — обновляем только счётчики
            Logger.Debug("Event types haven't changed, we'll reuse filters");
            UpdateFilterValues(_lastGeneratedFilters, eventList);
            return _lastGeneratedFilters;
        }

        // Типы изменились — пересоздаём фильтры
        Logger.Info("Event types have changed, we are generating new filters");

        var availableFilters = new List<FilterDescriptor>(_customFilters.Values);

        // Собираем только ОБЩИЕ поля (пересечение типов)
        var commonFields = AnalyzeCommonFields(eventList);

        foreach (var field in commonFields)
        {
            var filterId = _propertyAccessor.ToFilterId(field.PropertyPath);

            if (_customFilters.ContainsKey(filterId))
                continue;

            var descriptor = CreateFieldFilter(field, eventList);
            if (descriptor != null)
            {
                availableFilters.Add(descriptor);
            }
        }

        var result = availableFilters
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Priority)
            .ToList();

        // Сохраняем кэш
        _lastGeneratedFilters = result;
        _lastEventTypes = currentEventTypes;

        return result;
    }

    /// <summary>
    /// Обновляет значения и счётчики существующих фильтров
    /// </summary>
    private void UpdateFilterValues(List<FilterDescriptor> filters, List<EventBase> events)
    {
        foreach (var filter in filters)
        {
            switch (filter.FilterType)
            {
                case FilterType.EnumMultiSelect or FilterType.StringMultiSelect:
                    UpdateMultiSelectFilter(filter, events);
                    break;
                case FilterType.NumericRange or FilterType.DateTimeRange:
                    UpdateRangeFilter(filter, events);
                    break;
            }
        }
    }

    private void UpdateMultiSelectFilter(FilterDescriptor filter, List<EventBase> events)
    {
        var valueCounts = new Dictionary<object, int>();

        foreach (var evt in events)
        {
            var value = _propertyAccessor.GetValue(evt, filter.PropertyPath);
            if (value != null)
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        // Обновляем счётчики
        foreach (var item in filter.AvailableValues)
        {
            item.Count = valueCounts.GetValueOrDefault(item.Value, 0);
        }

        // Добавляем новые значения (если появились)
        var existingValues = filter.AvailableValues.Select(v => v.Value).ToHashSet();
        foreach (var (value, count) in valueCounts.Where(kv => !existingValues.Contains(kv.Key)))
        {
            var displayName = value is Enum ? GetEnumDisplayName(value) : value.ToString()!;
            filter.AvailableValues.Add(new FilterValueItem(value, displayName, count));
        }
    }

    private void UpdateRangeFilter(FilterDescriptor filter, List<EventBase> events)
    {
        var values = events
            .Select(evt => _propertyAccessor.GetValue(evt, filter.PropertyPath))
            .Where(v => v != null)
            .Cast<IComparable>()
            .ToList();

        if (values.Count > 0)
        {
            filter.MinValue = values.Min();
            filter.MaxValue = values.Max();

            // НЕ сбрасываем CurrentMinValue/CurrentMaxValue — пользователь мог их изменить
            // Только если они null
            filter.CurrentMinValue ??= filter.MinValue;
            filter.CurrentMaxValue ??= filter.MaxValue;
        }
    }

    public IEnumerable<EventBase> ApplyFilters(
        IEnumerable<EventBase> events,
        IEnumerable<FilterDescriptor> filters)
    {
        var activeFilters = filters.Where(f => f.IsActive).ToList();

        if (activeFilters.Count == 0)
            return events;

        Logger.Info("Apply {Count} activity filter(s)", activeFilters.Count);

        return events.Where(evt => activeFilters.All(filter => filter.FilterPredicate(evt)));
    }

    /// <summary>
    /// Собирает ОБЩИЕ поля (пересечение всех типов событий)
    /// </summary>
    private List<FilterFieldInfo> AnalyzeCommonFields(List<EventBase> events)
    {
        var eventTypes = events
            .Select(e => e.GetType())
            .Distinct()
            .ToList();

        if (eventTypes.Count == 0)
            return [];

        // Получаем поля для каждого типа
        var fieldsByType = eventTypes
            .Select(GetSortableFields)
            .ToList();

        if (fieldsByType.Count == 0)
            return [];

        // Находим пересечение полей
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

    private List<FilterFieldInfo> GetSortableFields(Type eventType)
    {
        if (_fieldCache.TryGetValue(eventType, out var cached))
            return cached;

        var fields = new List<FilterFieldInfo>();
        ScanProperties(eventType, string.Empty, fields, depth: 0);

        _fieldCache[eventType] = fields;
        return fields;
    }
    
    private void ScanProperties(Type type, string pathPrefix, List<FilterFieldInfo> results, int depth = 0)
    {
        if (depth > 3)
            return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var filterAttr = prop.GetCustomAttribute<FilterableFieldAttribute>();

            var path = string.IsNullOrEmpty(pathPrefix)
                ? prop.Name
                : $"{pathPrefix}.{prop.Name}";

            if (filterAttr != null)
            {
                results.Add(new FilterFieldInfo(
                    path,
                    filterAttr.DisplayName ?? prop.Name,
                    prop.PropertyType,
                    filterAttr.Category ?? "General",
                    filterAttr.Priority,
                    filterAttr.FilterType != 0 ? filterAttr.FilterType : DetermineFilterType(prop.PropertyType)));
            }

            if (ShouldScanNestedType(prop.PropertyType))
                ScanProperties(prop.PropertyType, path, results, depth + 1);
        }
    }

    private FilterType DetermineFilterType(Type propertyType)
    {
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
               type.Namespace?.StartsWith("FirebirdTraceParser") == true;
    }

    private FilterDescriptor? CreateFieldFilter(FilterFieldInfo field, List<EventBase> events)
    {
        var filterId = _propertyAccessor.ToFilterId(field.PropertyPath);

        return field.FilterType switch
        {
            FilterType.EnumMultiSelect => CreateEnumFilter(filterId, field, events),
            FilterType.StringMultiSelect => CreateStringFilter(filterId, field, events),
            FilterType.NumericRange => CreateNumericRangeFilter(filterId, field, events),
            FilterType.DateTimeRange => CreateDateTimeRangeFilter(filterId, field, events),
            _ => null
        };
    }

    #region Создание фильтров

    private FilterDescriptor CreateEnumFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        var valueCounts = new Dictionary<object, int>();

        foreach (var evt in events)
        {
            var value = _propertyAccessor.GetValue(evt, field.PropertyPath);
            if (value != null)
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        var availableValues = new ObservableCollection<FilterValueItem>();
        foreach (var (value, count) in valueCounts.OrderBy(kv => kv.Key.ToString()))
        {
            var displayName = GetEnumDisplayName(value);
            availableValues.Add(new FilterValueItem(value, displayName, count));
        }

        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.EnumMultiSelect,
            field.PropertyPath,
            evt => CheckEnumFilter(evt, field.PropertyPath, availableValues),
            field.Category,
            field.Priority);

        foreach (var item in availableValues)
            descriptor.AvailableValues.Add(item);

        // Инициализация FilteredValues
        descriptor.InitializeFilteredValues();

        return descriptor;
    }

    private FilterDescriptor CreateStringFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        var valueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var evt in events)
        {
            var value = _propertyAccessor.GetValue(evt, field.PropertyPath)?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        var availableValues = new ObservableCollection<FilterValueItem>();
        foreach (var (value, count) in valueCounts.OrderByDescending(kv => kv.Value).Take(100))
        {
            availableValues.Add(new FilterValueItem(value, value, count));
        }

        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.StringMultiSelect,
            field.PropertyPath,
            evt => CheckStringFilter(evt, field.PropertyPath, availableValues),
            field.Category,
            field.Priority);

        foreach (var item in availableValues)
            descriptor.AvailableValues.Add(item);

        // Инициализация FilteredValues
        descriptor.InitializeFilteredValues();
        
        return descriptor;
    }

    private FilterDescriptor CreateNumericRangeFilter(string id, FilterFieldInfo field, List<EventBase> events)
    {
        var values = events
            .Select(evt => _propertyAccessor.GetValue(evt, field.PropertyPath))
            .Where(v => v != null)
            .Cast<IComparable>()
            .ToList();

        if (values.Count == 0)
            return null!;

        var min = values.Min();
        var max = values.Max();

        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.NumericRange,
            field.PropertyPath,
            evt => true,
            field.Category,
            field.Priority)
        {
            MinValue = min,
            MaxValue = max,
            CurrentMinValue = min,
            CurrentMaxValue = max
        };

        // Динамический предикат (читает текущие значения)
        descriptor.UpdatePredicate(evt =>
        {
            var value = _propertyAccessor.GetValue(evt, descriptor.PropertyPath) as IComparable;
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
            .Select(evt => _propertyAccessor.GetValue(evt, field.PropertyPath))
            .Where(v => v != null)
            .Cast<DateTime>()
            .ToList();
        
        

        if (values.Count == 0)
            return null!;

        var min = values.Min();
        var max = values.Max();

        var descriptor = new FilterDescriptor(
            id,
            field.DisplayName,
            FilterType.DateTimeRange,
            field.PropertyPath,
            evt => false,
            field.Category,
            field.Priority)
        {
            MinValue = min,
            MaxValue = max,
            CurrentMinValue = min,
            CurrentMaxValue = max
        };

        descriptor.UpdatePredicate(evt =>
        {
            var value = _propertyAccessor.GetValue(evt, descriptor.PropertyPath);
            if (value is not DateTime dateTime)
                return false;

            var currentMin = DateTime.Parse(descriptor.CurrentMinValue.ToString() ?? throw new InvalidOperationException());
            var currentMax = DateTime.Parse(descriptor.CurrentMaxValue.ToString() ?? throw new InvalidOperationException());
            
            if (dateTime < currentMin)
                return false;

            if (dateTime > currentMax)
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
            return true;

        var value = _propertyAccessor.GetValue(evt, propertyPath);
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

        var value = _propertyAccessor.GetValue(evt, propertyPath)?.ToString();
        return value != null && selectedValues.Contains(value);
    }

    #endregion

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