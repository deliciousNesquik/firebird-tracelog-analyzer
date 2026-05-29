namespace FirebirdTraceParser.Core.Attributes;

/// <summary>
/// Маркирует свойство как доступное для фильтрации.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FilterableFieldAttribute : Attribute
{
    /// <summary>Отображаемое имя поля в UI</summary>
    public string DisplayName { get; }
    
    /// <summary>Приоритет отображения (меньше = выше)</summary>
    public int Priority { get; init; } = 100;
    
    /// <summary>Категория фильтра (например, "Общие", "Подключение")</summary>
    public string Category { get; init; } = "General";
    
    /// <summary>Тип фильтра (автоопределяется, но можно переопределить)</summary>
    public FilterType FilterType { get; init; } = FilterType.Auto;

    public FilterableFieldAttribute(string displayName)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }
}

/// <summary>
/// Типы фильтров для автоматического создания UI.
/// </summary>
public enum FilterType
{
    /// <summary>Автоматическое определение по типу свойства</summary>
    Auto,
    
    /// <summary>Enum → список чекбоксов</summary>
    EnumMultiSelect,
    
    /// <summary>String → множественный выбор + поиск</summary>
    StringMultiSelect,
    
    /// <summary>Числовой диапазон (int, long, decimal)</summary>
    NumericRange,
    
    /// <summary>Диапазон дат</summary>
    DateTimeRange,
    
    /// <summary>Boolean → переключатель</summary>
    Boolean,
    
    /// <summary>Полнотекстовый поиск (для SQL, длинных текстов)</summary>
    TextSearch
}