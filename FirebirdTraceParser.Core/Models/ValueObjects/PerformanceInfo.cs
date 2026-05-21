using FirebirdTraceParser.Core.Attributes;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Общая информация о производительности выполнения SQL‑операции.
/// Соответствует Python PerformanceInfo.
/// </summary>
public sealed record PerformanceInfo
{
    /// <summary>Время выполнения запроса в миллисекундах</summary>
    [SortableField("Время выполнения (мс)", Priority = 30, Category = "Производительность")]
    public required int ExecuteMs { get; init; }
    
    /// <summary>Количество выданных записей (fetch)</summary>
    [SortableField("Количество fetch", Priority = 31, Category = "Производительность")]
    public required int FetchCount { get; init; }
    
    /// <summary>Количество операций чтения</summary>
    [SortableField("Количество read", Priority = 32, Category = "Производительность")]
    public required int ReadCount { get; init; }
    
    /// <summary>Количество операций записи</summary>
    [SortableField("Количество write", Priority = 33, Category = "Производительность")]
    public required int WriteCount { get; init; }
    
    /// <summary>Количество внутренних отметок (mark)</summary>
    public required int MarkCount { get; init; }
}