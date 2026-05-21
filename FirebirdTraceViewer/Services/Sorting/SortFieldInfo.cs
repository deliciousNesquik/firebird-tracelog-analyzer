namespace FirebirdTraceViewer.Services.Sorting;

/// <summary>
/// Метаданные поля, доступного для сортировки.
/// </summary>
public sealed class SortFieldInfo
{
    /// <summary>Путь к свойству (например, "Timestamp", "Attachment.User")</summary>
    public string PropertyPath { get; }
    
    /// <summary>Отображаемое имя</summary>
    public string DisplayName { get; }
    
    /// <summary>Категория</summary>
    public string Category { get; }
    
    /// <summary>Приоритет</summary>
    public int Priority { get; }
    
    /// <summary>Тип свойства</summary>
    public Type PropertyType { get; }

    public SortFieldInfo(
        string propertyPath,
        string displayName,
        Type propertyType,
        string category,
        int priority)
    {
        PropertyPath = propertyPath;
        DisplayName = displayName;
        PropertyType = propertyType;
        Category = category;
        Priority = priority;
    }
}