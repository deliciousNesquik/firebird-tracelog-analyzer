namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Статистика по одной таблице.
/// </summary>
public sealed record PerformanceTableItem
{
    public required string TableName { get; init; }
    public required int NaturalCount { get; init; }
    public required int IndexCount { get; init; }
    public required int UpdateCount { get; init; }
    public required int InsertCount { get; init; }
    public required int DeleteCount { get; init; }
    public required int BackoutCount { get; init; }
    public required int PurgeCount { get; init; }
    public required int ExpungeCount { get; init; }
}

/// <summary>
/// Таблица статистики производительности.
/// </summary>
public sealed record PerformanceTable
{
    /// <summary>Список статистик по таблицам</summary>
    public IReadOnlyList<PerformanceTableItem>? Items { get; init; }
}