namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Общая информация о производительности выполнения SQL‑операции.
/// Соответствует Python PerformanceInfo.
/// </summary>
public sealed record PerformanceInfo
{
    /// <summary>Время выполнения запроса в миллисекундах</summary>
    public required int ExecuteMs { get; init; }
    
    /// <summary>Количество выданных записей (fetch)</summary>
    public required int FetchCount { get; init; }
    
    /// <summary>Количество операций чтения</summary>
    public required int ReadCount { get; init; }
    
    /// <summary>Количество операций записи</summary>
    public required int WriteCount { get; init; }
    
    /// <summary>Количество внутренних отметок (mark)</summary>
    public required int MarkCount { get; init; }
}