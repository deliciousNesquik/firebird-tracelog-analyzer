using System.Globalization;
using System.Text.RegularExpressions;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Engine;
using FirebirdTraceParser.Core.Parsing.Utils;
using NLog;

namespace FirebirdTraceParser.Core.Parsing.Handlers;

/// <summary>
///     Обработчик событий по умолчанию.
/// </summary>
public sealed class DefaultEventHandler : IEventHandler
{
    private readonly ILogger _logger;
    private readonly ParseOptions _options;
    
    private static readonly Dictionary<string, EventType> EventTypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TRACE_INIT"] = EventType.TraceInit,
        ["TRACE_FINI"] = EventType.TraceFinish,
        ["ATTACH_DATABASE"] = EventType.AttachDatabase,
        ["DETACH_DATABASE"] = EventType.DetachDatabase,
        ["EXECUTE_STATEMENT_START"] = EventType.ExecuteStatementStart,
        ["EXECUTE_STATEMENT_FINISH"] = EventType.ExecuteStatementFinish,
        ["EXECUTE_PROCEDURE_START"] = EventType.ExecuteProcedureStart,
        ["EXECUTE_PROCEDURE_FINISH"] = EventType.ExecuteProcedureFinish,
        ["EXECUTE_TRIGGER_START"] = EventType.ExecuteTriggerStart,
        ["EXECUTE_TRIGGER_FINISH"] = EventType.ExecuteTriggerFinish,
        ["FAILED EXECUTE_STATEMENT_FINISH"] = EventType.FailedExecuteStatementFinish,
        ["FAILED EXECUTE_PROCEDURE_FINISH"] = EventType.FailedExecuteProcedureFinish,
        ["FAILED EXECUTE_TRIGGER_FINISH"] = EventType.FailedExecuteTriggerFinish,
        ["ERROR AT JR"] = EventType.ErrorAtJr,
        ["ERROR AT JS"] = EventType.ErrorAtJs
    };

    private readonly Dictionary<EventType, Func<Match, IReadOnlyList<string>, IReadOnlyDictionary<string, Regex>, EventBase?>> _handlers;

    public DefaultEventHandler(ILogger logger, ParseOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? ParseOptions.Default;

        _handlers = new Dictionary<EventType, Func<Match, IReadOnlyList<string>, IReadOnlyDictionary<string, Regex>, EventBase?>>
        {
            [EventType.TraceInit] = HandleTraceInit,
            [EventType.TraceFinish] = HandleTraceFinish,
            [EventType.AttachDatabase] = HandleAttach,
            [EventType.DetachDatabase] = HandleDetach,
            [EventType.ExecuteStatementStart] = HandleStatementStart,
            [EventType.ExecuteStatementFinish] = HandleStatementFinish,
            [EventType.ExecuteProcedureStart] = HandleProcedureStart,
            [EventType.ExecuteProcedureFinish] = HandleProcedureFinish,
            [EventType.ExecuteTriggerStart] = HandleTriggerStart,
            [EventType.ExecuteTriggerFinish] = HandleTriggerFinish,
            [EventType.FailedExecuteProcedureFinish] = HandleFailedProcedureFinish,
            [EventType.FailedExecuteStatementFinish] = HandleFailedStatementFinish,
            [EventType.FailedExecuteTriggerFinish] = HandleFailedTriggerFinish,
        };
    }

    public EventBase? Handle(Match blockHeader, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var eventTypeStr = blockHeader.Groups["event_type"].Value;
    
        _logger.Debug("Handling event: {EventType}, body lines: {LineCount}", eventTypeStr, bodyLines.Count);
        
        if (!EventTypeMapping.TryGetValue(eventTypeStr, out var eventType))
        {
            _logger.Warn("Unknown event type: '{EventType}'", eventTypeStr);
            return null;
        }

        if (!_handlers.TryGetValue(eventType, out var handler))
        {
            _logger.Debug("No handler for event type: {EventType}", eventType);
            return null;
        }
    
        var result = handler(blockHeader, bodyLines, rules);
    
        if (result == null)
            _logger.Warn("Handler returned null for {EventType}", eventType);
        else
            _logger.Debug("Successfully parsed {EventType}", eventType);

        return result;
    }

    // ==================== Common Event Metadata Parsing ====================
    
    /// <summary>
    /// Извлекает общие метаданные заголовка события (timestamp, trace_id, hex_trace_id).
    /// </summary>
    private static (DateTime Timestamp, int TraceId, string HexTraceId) ParseEventMetadata(Match header)
    {
        return (
            Timestamp: DateTime.Parse(header.Groups["ts"].Value, CultureInfo.InvariantCulture),
            TraceId: int.Parse(header.Groups["trace_id"].ValueSpan),
            HexTraceId: StringPool.Intern(header.Groups["hex_trace_id"].Value)
        );
    }

    /// <summary>
    /// Создает PerformanceInfo по умолчанию (все нули).
    /// </summary>
    private static PerformanceInfo CreateDefaultPerformance() => new()
    {
        ExecuteMs = 0,
        FetchCount = 0,
        ReadCount = 0,
        WriteCount = 0,
        MarkCount = 0
    };

    // ==================== Handlers ====================

    private TraceInitEvent? HandleTraceInit(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var session = ParseSessionInfo(bodyLines, rules);
        if (session is null) return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new TraceInitEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.TraceInit,
            Session = session
        };
    }

    private TraceFinishEvent? HandleTraceFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var session = ParseSessionInfo(bodyLines, rules);
        if (session is null) return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new TraceFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.TraceFinish,
            Session = session
        };
    }

    private AttachDatabaseEvent? HandleAttach(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var attachment = ParseAttachmentInfo(bodyLines, rules);
        if (attachment is null) return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new AttachDatabaseEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.AttachDatabase,
            Attachment = attachment
        };
    }

    private DetachDatabaseEvent? HandleDetach(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var attachment = ParseAttachmentInfo(bodyLines, rules);
        if (attachment is null) return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new DetachDatabaseEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.DetachDatabase,
            Attachment = attachment
        };
    }

    private StatementStartEvent? HandleStatementStart(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseStatementData(bodyLines, rules, includePerformance: false);
        if (data.Attachment is null || data.Transaction is null || data.StatementId is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new StatementStartEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.ExecuteStatementStart,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            StatementId = data.StatementId.Value,
            Sql = data.Sql ?? string.Empty,
            Parameters = data.Params
        };
    }

    private StatementFinishEvent? HandleStatementFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseStatementData(bodyLines, rules, includePerformance: true);
        if (data.Attachment is null || data.Transaction is null || data.StatementId is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new StatementFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.ExecuteStatementFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            StatementId = data.StatementId.Value,
            Sql = data.Sql ?? string.Empty,
            Parameters = data.Params,
            Performance = data.Performance ?? CreateDefaultPerformance(),
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }
    
    private FailedStatementFinishEvent? HandleFailedStatementFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseStatementData(bodyLines, rules, includePerformance: true);
        if (data.Attachment is null || data.Transaction is null || data.StatementId is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new FailedStatementFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.FailedExecuteStatementFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            StatementId = data.StatementId.Value,
            Sql = data.Sql ?? string.Empty,
            Parameters = data.Params,
            Performance = data.Performance ?? CreateDefaultPerformance(),
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }

    private ProcedureStartEvent? HandleProcedureStart(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseProcedureData(bodyLines, rules, includePerformance: false);
        if (data.Attachment is null || data.Transaction is null || data.ProcedureName is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new ProcedureStartEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.ExecuteProcedureStart,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            ProcedureName = data.ProcedureName,
            Parameters = data.Params
        };
    }

    private ProcedureFinishEvent? HandleProcedureFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseProcedureData(bodyLines, rules, includePerformance: true);
        if (data.Attachment is null || data.Transaction is null || data.ProcedureName is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new ProcedureFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.ExecuteProcedureFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            ProcedureName = data.ProcedureName,
            Parameters = data.Params,
            Performance = data.Performance ?? CreateDefaultPerformance(),
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }
    
    private FailedProcedureFinishEvent? HandleFailedProcedureFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseProcedureData(bodyLines, rules, includePerformance: true);
        if (data.Attachment is null || data.Transaction is null || data.ProcedureName is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new FailedProcedureFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.FailedExecuteProcedureFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            ProcedureName = data.ProcedureName,
            Parameters = data.Params,
            Performance = data.Performance ?? CreateDefaultPerformance(),
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }

    private TriggerStartEvent? HandleTriggerStart(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseTriggerData(bodyLines, rules, includePerformance: false);
        if (data.Attachment is null || data.Transaction is null ||
            data.TriggerName is null || data.Table is null || data.Timing is null || data.Event is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new TriggerStartEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.ExecuteTriggerStart,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            TriggerName = data.TriggerName,
            Table = data.Table,
            Timing = data.Timing,
            Event = data.Event
        };
    }

    private TriggerFinishEvent? HandleTriggerFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseTriggerData(bodyLines, rules, includePerformance: true);
        if (data.Attachment is null || data.Transaction is null ||
            data.TriggerName is null || data.Table is null || data.Timing is null || data.Event is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new TriggerFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.ExecuteTriggerFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            TriggerName = data.TriggerName,
            Table = data.Table,
            Timing = data.Timing,
            Event = data.Event,
            Performance = data.Performance ?? CreateDefaultPerformance(),
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }
    
    private FailedTriggerFinishEvent? HandleFailedTriggerFinish(Match header, IReadOnlyList<string> bodyLines, IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseTriggerData(bodyLines, rules, includePerformance: true);
        if (data.Attachment is null || data.Transaction is null ||
            data.TriggerName is null || data.Table is null || data.Timing is null || data.Event is null)
            return null;

        var (timestamp, traceId, hexTraceId) = ParseEventMetadata(header);

        return new FailedTriggerFinishEvent
        {
            Timestamp = timestamp,
            TraceId = traceId,
            HexTraceId = hexTraceId,
            EventType = EventType.FailedExecuteTriggerFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            TriggerName = data.TriggerName,
            Table = data.Table,
            Timing = data.Timing,
            Event = data.Event,
            Performance = data.Performance ?? CreateDefaultPerformance(),
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }

    // ==================== Parsing helpers ====================
    
    private static TraceSessionInfo? ParseSessionInfo(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules)
    {
        foreach (var line in lines)
        {
            var m = rules["session"].Match(line);
            if (m.Success)
            {
                int sessionId = int.Parse(m.Groups["session_id"].ValueSpan);
                return TraceSessionInfoPool.Intern(sessionId);
            }
        }

        return null;
    }

    private static AttachmentInfo? ParseAttachmentInfo(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules)
    {
        Match? am = null;
        Match? pm = null;

        foreach (var line in lines)
        {
            if (am is null)
            {
                var match = rules["attachment"].Match(line);
                if (match.Success) am = match;
            }

            if (pm is null)
            {
                var match = rules["process"].Match(line);
                if (match.Success) pm = match;
            }

            if (am is not null && pm is not null) break;
        }

        if (am is null) return null;

        var attachmentId = int.Parse(am.Groups["attachment_id"].ValueSpan);
    
        if (AttachmentInfoPool.TryGet(attachmentId, out var cachedInfo))
            return cachedInfo;

        var newInfo = new AttachmentInfo
        {
            AttachmentId = attachmentId,
            DatabasePath = StringPool.Intern(am.Groups["database_path"].Value),
            User = StringPool.Intern(am.Groups["user"].Value),
            Role = StringPool.Intern(am.Groups["role"].Value),
            Charset = StringPool.Intern(am.Groups["charset"].Value),
            Protocol = StringPool.Intern(am.Groups["protocol"].Value.Trim()),
            Address = am.Groups["address"].Success ? StringPool.Intern(am.Groups["address"].Value) : StringPool.Intern("<internal>"),
            Port = am.Groups["port"].Success ? int.Parse(am.Groups["port"].ValueSpan) : 0,
            ProcessPath = pm is not null ? StringPool.Intern(pm.Groups["process_path"].Value) : null,
            ProcessId = pm is not null ? int.Parse(pm.Groups["process_id"].ValueSpan) : null
        };

        return AttachmentInfoPool.Add(newInfo);
    }

    private static TransactionInfo? ParseTransactionInfo(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules)
    {
        foreach (var line in lines)
        {
            if (!line.Contains("(TRA_")) continue;

            var m = rules["transaction"].Match(line);
            if (!m.Success) continue;

            var tid = int.Parse(m.Groups[1].ValueSpan);
            var rawParams = m.Groups[2].Value;

            var parts = rawParams.Split('|')
                .Select(p => p.Trim().ToUpperInvariant())
                .Where(p => !string.IsNullOrEmpty(p) && p != "NONE" && p != "(NONE)")
                .ToList();

            while (parts.Count < 4)
                parts.Add("NONE");

            return new TransactionInfo
            {
                TransactionId = tid,
                IsolationLevel = StringPool.Intern(parts[0]),
                ConsistencyMode = StringPool.Intern(parts[1]),
                LockMode = StringPool.Intern(parts[2]),
                AccessMode = StringPool.Intern(parts[3])
            };
        }

        return null;
    }
    
    /// <summary>
    /// Парсит одну строку SQL-параметра и возвращает объект SqlParameters или null.
    /// </summary>
    private static SqlParameters? ParseSqlParameter(string line, IReadOnlyDictionary<string, Regex> rules)
    {
        var paramM = rules["parameters"].Match(line);
        if (!paramM.Success) 
            return null;

        var value = paramM.Groups["value"].Success
            ? paramM.Groups["value"].Value
            : paramM.Groups["value_null"].Value;

        return new SqlParameters
        {
            Name = StringPool.Intern(paramM.Groups["name"].Value),
            Dtype = StringPool.Intern(paramM.Groups["dtype"].Value),
            Value = StringPool.Intern(value)
        };
    }

    // ==================== Data Records ====================

    private sealed record StatementData(
        AttachmentInfo? Attachment,
        TransactionInfo? Transaction,
        int? StatementId,
        string? Sql,
        IReadOnlyList<SqlParameters> Params,
        PerformanceInfo? Performance,
        PerformanceTable? PerformanceTable);

    private sealed record ProcedureData(
        AttachmentInfo? Attachment,
        TransactionInfo? Transaction,
        string? ProcedureName,
        IReadOnlyList<SqlParameters> Params,
        PerformanceInfo? Performance,
        PerformanceTable? PerformanceTable);

    private sealed record TriggerData(
        AttachmentInfo? Attachment,
        TransactionInfo? Transaction,
        string? TriggerName,
        string? Table,
        string? Timing,
        string? Event,
        PerformanceInfo? Performance,
        PerformanceTable? PerformanceTable);

    // ==================== Complex Parsers ====================

    private static StatementData ParseStatementData(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules, bool includePerformance)
    {
        var attachment = ParseAttachmentInfo(lines, rules);
        TransactionInfo? transaction = null;
        int? statementId = null;
        string? sql = null;
        var paramsList = new List<SqlParameters>();
        PerformanceInfo? performance = null;
        PerformanceTable? performanceTable = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (transaction is null)
            {
                transaction = ParseTransactionInfo(new[] { line }, rules);
                if (transaction is not null) continue;
            }

            var sm = rules["statement"].Match(line);
            if (sm.Success)
            {
                statementId = int.Parse(sm.Groups["statement_id"].ValueSpan);
                continue;
            }

            if (line.Trim().StartsWith("-----"))
            {
                var sqlLines = new List<string>();
                i++;
                while (i < lines.Count)
                {
                    var l = lines[i];
                    if (rules["parameters"].IsMatch(l) ||
                        (includePerformance && (rules["fetched"].IsMatch(l) || rules["performance"].IsMatch(l))))
                        break;

                    sqlLines.Add(l);
                    i++;
                }

                sql = StringPool.Intern(string.Join("\n", sqlLines).Trim());
                i--;
                continue;
            }

            var param = ParseSqlParameter(line, rules);
            if (param is not null)
            {
                paramsList.Add(param);
                continue;
            }

            if (includePerformance && performance is null)
            {
                performance = ParsePerformance(new[] { line }, rules);
                if (performance is not null) continue;
            }

            if (includePerformance && performanceTable is null)
            {
                var slice = lines.Skip(i).ToArray();
                performanceTable = PerformanceTableParser.ParsePerformanceTable(slice, rules);
            }
        }

        return new StatementData(attachment, transaction, statementId, sql, paramsList, performance, performanceTable);
    }

    private static ProcedureData ParseProcedureData(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules, bool includePerformance)
    {
        var attachment = ParseAttachmentInfo(lines, rules); // ← ИСПРАВЛЕНО: добавлен парсинг
        TransactionInfo? transaction = null;
        string? procedureName = null;
        var paramsList = new List<SqlParameters>();
        PerformanceInfo? performance = null;
        PerformanceTable? performanceTable = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (transaction is null)
            {
                transaction = ParseTransactionInfo(new[] { line }, rules);
                if (transaction is not null) continue;
            }

            var pmProc = rules["procedure"].Match(line);
            if (pmProc.Success)
            {
                procedureName = StringPool.Intern(pmProc.Groups["procedure_name"].Value);
                continue;
            }

            var param = ParseSqlParameter(line, rules);
            if (param is not null)
            {
                paramsList.Add(param);
                continue;
            }

            if (includePerformance && performance is null)
                performance = ParsePerformance(new[] { line }, rules);
        }

        return new ProcedureData(attachment, transaction, procedureName, paramsList, performance, performanceTable);
    }

    private static TriggerData ParseTriggerData(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules, bool includePerformance)
    {
        var attachment = ParseAttachmentInfo(lines, rules);
        TransactionInfo? transaction = null;
        string? triggerName = null;
        string? table = null;
        string? timing = null;
        string? eventType = null;
        PerformanceInfo? performance = null;
        PerformanceTable? performanceTable = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (transaction is null)
            {
                transaction = ParseTransactionInfo(new[] { line }, rules);
                if (transaction is not null) continue;
            }

            var tm = rules["trigger"].Match(line);
            if (tm.Success)
            {
                triggerName = StringPool.Intern(tm.Groups["trigger_name"].Value);
                table = StringPool.Intern(tm.Groups["table"].Value);
                timing = StringPool.Intern(tm.Groups["timing"].Value);
                eventType = StringPool.Intern(tm.Groups["event"].Value);
                continue;
            }

            if (includePerformance && performance is null)
                performance = ParsePerformance(new[] { line }, rules);
        }

        return new TriggerData(attachment, transaction, triggerName, table, timing, eventType, performance, performanceTable);
    }

    private static PerformanceInfo? ParsePerformance(IReadOnlyList<string> lines, IReadOnlyDictionary<string, Regex> rules)
    {
        var fetchCount = 0;

        foreach (var line in lines)
        {
            var mFetched = rules["fetched"].Match(line);
            if (mFetched.Success)
                fetchCount = int.Parse(mFetched.Groups["fetch_count"].ValueSpan);

            var mPerf = rules["performance"].Match(line);
            if (mPerf.Success)
                return new PerformanceInfo
                {
                    ExecuteMs = int.Parse(mPerf.Groups["execute_ms"].ValueSpan),
                    FetchCount = fetchCount,
                    ReadCount = mPerf.Groups["read"].Success ? int.Parse(mPerf.Groups["read"].ValueSpan) : 0,
                    WriteCount = mPerf.Groups["write"].Success ? int.Parse(mPerf.Groups["write"].ValueSpan) : 0,
                    MarkCount = mPerf.Groups["mark"].Success ? int.Parse(mPerf.Groups["mark"].ValueSpan) : 0
                };
        }

        return null;
    }
}