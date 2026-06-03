namespace FirebirdTraceAnalyzer.Models.Reports;

/// <summary>
/// Конфигурация фильтра для отчёта
/// </summary>
public sealed class ReportFilterConfig
{
    /// <summary>ID фильтра (например, "filter_eventtype")</summary>
    public required string FilterId { get; init; }
    
    /// <summary>Название фильтра (для отображения)</summary>
    public required string DisplayName { get; init; }
    
    /// <summary>Активен ли фильтр</summary>
    public bool IsActive { get; init; }
    
    /// <summary>Выбранные значения (для Enum/String фильтров)</summary>
    public List<object>? SelectedValues { get; init; }
    
    /// <summary>Минимальное значение (для Range фильтров)</summary>
    public object? MinValue { get; init; }
    
    /// <summary>Максимальное значение (для Range фильтров)</summary>
    public object? MaxValue { get; init; }
}