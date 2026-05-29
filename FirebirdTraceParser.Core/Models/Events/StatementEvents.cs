using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий SQL statement.
/// </summary>
public class StatementEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }
    public required int StatementId { get; init; }
    public required string Sql { get; init; }
    public required IReadOnlyList<SqlParam> Parameters { get; init; }
}

/// <summary>
/// Событие начала выполнения statement.
/// </summary>
public sealed class StatementStartEvent : StatementEventBase;

/// <summary>
/// Событие завершения выполнения statement.
/// </summary>
public sealed class StatementFinishEvent : StatementEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}

public sealed class FailedStatementFinishEvent : StatementEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}