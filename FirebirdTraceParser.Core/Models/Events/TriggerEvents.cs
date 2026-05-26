using System.Runtime.InteropServices;
using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий триггеров.
/// Соответствует Python TriggerEventBase.
/// </summary>
public class TriggerEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }
    
    private string _triggerName = string.Empty;

    public required string TriggerName
    {
        get => _triggerName;
        init => _triggerName = StringPool.Intern(value);
    }
    
    private string _table = string.Empty;

    public required string Table
    {
        get => _table;
        init => _table = StringPool.Intern(value);
    }
    
    private string _timing = string.Empty;

    public required string Timing
    {
        get => _timing;
        init => _timing = StringPool.Intern(value);
    }
    
    private string _event =  string.Empty;

    public required string Event
    {
        get => _event;
        init => _event = StringPool.Intern(value);
    }
}

/// <summary>
/// Событие начала выполнения триггера.
/// Соответствует Python TriggerStartEvent.
/// </summary>
public sealed class TriggerStartEvent : TriggerEventBase;

/// <summary>
/// Событие завершения выполнения триггера.
/// Соответствует Python TriggerFinishEvent.
/// </summary>
public sealed class TriggerFinishEvent : TriggerEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}