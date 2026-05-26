using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий хранимых процедур.
/// Соответствует Python ProcedureEventBase.
/// </summary>
public class ProcedureEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }
    public required string ProcedureName { get; init; }
    public required IReadOnlyList<SqlParam> Params { get; init; }
}

/// <summary>
/// Событие начала выполнения хранимой процедуры.
/// Соответствует Python ProcedureStartEvent.
/// </summary>
public sealed class ProcedureStartEvent : ProcedureEventBase;

/// <summary>
/// Событие завершения выполнения хранимой процедуры.
/// Соответствует Python ProcedureFinishEvent.
/// </summary>
public sealed class ProcedureFinishEvent : ProcedureEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}