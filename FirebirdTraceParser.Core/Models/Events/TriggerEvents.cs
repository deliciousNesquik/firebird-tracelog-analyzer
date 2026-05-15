using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий триггеров.
/// Соответствует Python TriggerEventBase.
/// </summary>
public abstract record TriggerEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }
    public required string TriggerName { get; init; }
    public required string Table { get; init; }
    public required string Timing { get; init; }
    public required string Event { get; init; }
}

/// <summary>
/// Событие начала выполнения триггера.
/// Соответствует Python TriggerStartEvent.
/// </summary>
public sealed record TriggerStartEvent : TriggerEventBase;

/// <summary>
/// Событие завершения выполнения триггера.
/// Соответствует Python TriggerFinishEvent.
/// </summary>
public sealed record TriggerFinishEvent : TriggerEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}