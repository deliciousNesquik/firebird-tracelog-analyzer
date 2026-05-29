using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий хранимых процедур.
/// </summary>
public class ProcedureEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }

    public required string ProcedureName { get; init; }

    public required IReadOnlyList<SqlParam> Parameters { get; init; }
}

/// <summary>
/// Событие начала выполнения хранимой процедуры.
/// </summary>
public sealed class ProcedureStartEvent : ProcedureEventBase;

/// <summary>
/// Событие завершения выполнения хранимой процедуры.
/// </summary>
public sealed class ProcedureFinishEvent : ProcedureEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}

public sealed class FailedProcedureFinishEvent : ProcedureEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}