using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий SQL statement.
/// Соответствует Python StatementEventBase.
/// </summary>
public abstract record StatementEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }
    public required int StatementId { get; init; }
    public required string Sql { get; init; }
    public required IReadOnlyList<SqlParam> Params { get; init; }
}

/// <summary>
/// Событие начала выполнения statement.
/// Соответствует Python StatementStartEvent.
/// </summary>
public sealed record StatementStartEvent : StatementEventBase;

/// <summary>
/// Событие завершения выполнения statement.
/// Соответствует Python StatementFinishEvent.
/// </summary>
public sealed record StatementFinishEvent : StatementEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}