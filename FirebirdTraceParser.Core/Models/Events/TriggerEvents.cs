using System.Runtime.InteropServices;
using FirebirdTraceParser.Core.Attributes;
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
    
    [SortableField("Trigger name", Priority = 2, Category = "Triggers")]
    [FilterableField("Trigger name", Category = "Triggers", FilterType =  FilterType.StringMultiSelect)]
    public required string TriggerName { get; init; }
    
    [SortableField("Table name", Priority = 2, Category = "Triggers")]
    [FilterableField("Table name", Category = "Triggers", FilterType =  FilterType.StringMultiSelect)]
    public required string Table { get; init; }
    
    [SortableField("Timing", Priority = 2, Category = "Triggers")]
    [FilterableField("Timing", Category = "Triggers", FilterType =  FilterType.StringMultiSelect)]
    public required string Timing { get; init; }
    
    [SortableField("Event", Priority = 2, Category = "Triggers")]
    [FilterableField("Event", Category = "Triggers", FilterType =  FilterType.StringMultiSelect)]
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