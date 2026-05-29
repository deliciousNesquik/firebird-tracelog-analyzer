namespace FirebirdTraceParser.Core.Attributes;

/// <summary>
/// Маркирует свойство как доступное для сортировки.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SortableFieldAttribute : Attribute
{
    /// <summary>Отображаемое имя поля в UI</summary>
    public string DisplayName { get; }
    
    /// <summary>Приоритет отображения (меньше = выше)</summary>
    public int Priority { get; init; } = 100;
    
    /// <summary>Категория сортировки (например, "Общие", "Производительность")</summary>
    public string Category { get; init; } = "General";

    /// <summary>Является ли сортировкой по умолчанию</summary>
    public bool IsDefault { get; init; } = false;

    public SortableFieldAttribute(string displayName)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }
}