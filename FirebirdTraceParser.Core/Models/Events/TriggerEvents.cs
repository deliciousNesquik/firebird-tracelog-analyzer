using System.Runtime.InteropServices;
using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий триггеров.
/// </summary>
public class TriggerEventBase : EventBase
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
/// </summary>
public sealed class TriggerStartEvent : TriggerEventBase;

/// <summary>
/// Событие завершения выполнения триггера.
/// </summary>
public sealed class TriggerFinishEvent : TriggerEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}

public sealed class FailedTriggerFinishEvent : TriggerEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}