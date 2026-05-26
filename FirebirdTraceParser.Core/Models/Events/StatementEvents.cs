using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовый класс для событий SQL statement.
/// Соответствует Python StatementEventBase.
/// </summary>
public class StatementEventBase : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
    public required TransactionInfo Transaction { get; init; }
    public required int StatementId { get; init; }
    
    private string _sql = string.Empty;

    public required string Sql
    {
        get => _sql;
        init => _sql = StringPool.Intern(value);
    }
    public required IReadOnlyList<SqlParam> Params { get; init; }
}

/// <summary>
/// Событие начала выполнения statement.
/// Соответствует Python StatementStartEvent.
/// </summary>
public sealed class StatementStartEvent : StatementEventBase;

/// <summary>
/// Событие завершения выполнения statement.
/// Соответствует Python StatementFinishEvent.
/// </summary>
public sealed class StatementFinishEvent : StatementEventBase
{
    public required PerformanceInfo Performance { get; init; }
    public PerformanceTable? PerformanceTable { get; init; }
}