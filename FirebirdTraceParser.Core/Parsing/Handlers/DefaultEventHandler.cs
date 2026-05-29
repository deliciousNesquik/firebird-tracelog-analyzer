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
///     Обработчик событий по умолчанию. Соответствует логике Python EventHandler.
/// </summary>
public sealed class DefaultEventHandler : IEventHandler
{
    private readonly ILogger _logger;
    private readonly ParseOptions _options;
    
    private static readonly IReadOnlyDictionary<string, EventType> EventTypeMapping = new Dictionary<string, EventType>(StringComparer.OrdinalIgnoreCase)
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

    private readonly
        Dictionary<EventType, Func<Match, IReadOnlyList<string>, IReadOnlyDictionary<string, Regex>, EventBase?>>
        _handlers;

    public DefaultEventHandler(ILogger logger, ParseOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? ParseOptions.Default;

        _handlers =
            new Dictionary<EventType,
                Func<Match, IReadOnlyList<string>, IReadOnlyDictionary<string, Regex>, EventBase?>>
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

    public EventBase? Handle(
        Match blockHeader,
        IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var eventTypeStr = blockHeader.Groups["event_type"].Value.Trim();
    
        _logger.Debug("Handling event: {EventType}, body lines: {LineCount}", eventTypeStr, bodyLines.Count);

        /*if (!Enum.TryParse<EventType>(eventTypeStr.Replace(" ", ""), out var eventType))
        {
            _logger.Warn("Unknown event type: '{EventType}' (after replace: '{Replaced}')", 
                eventTypeStr, eventTypeStr.Replace(" ", ""));
            return null;
        }
        */
        
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
        {
            _logger.Warn("Handler returned null for {EventType}", eventType);
        }
        else
        {
            _logger.Debug("Successfully parsed {EventType}", eventType);
        }

        return result;
    }

    // ==================== Helpers ====================
    private static DateTime ParseTimestamp(string ts)
    {
        return DateTime.Parse(ts, CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string? value)
    {
        return string.IsNullOrEmpty(value) ? 0 : int.Parse(value, CultureInfo.InvariantCulture);
    }

    // ==================== Handlers ====================

    private TraceInitEvent? HandleTraceInit(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var session = ParseSessionInfo(bodyLines, rules);
        if (session is null) return null;

        return new TraceInitEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.TraceInit,
            Session = session
        };
    }

    private TraceFinishEvent? HandleTraceFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var session = ParseSessionInfo(bodyLines, rules);
        if (session is null) return null;

        return new TraceFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.TraceFinish,
            Session = session
        };
    }

    private AttachDatabaseEvent? HandleAttach(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var attachment = ParseAttachmentInfoWithProcess(bodyLines, rules);
        if (attachment is null) return null;

        return new AttachDatabaseEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.AttachDatabase,
            Attachment = attachment
        };
    }

    private DetachDatabaseEvent? HandleDetach(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var attachment = ParseAttachmentInfoWithProcess(bodyLines, rules);
        if (attachment is null) return null;

        return new DetachDatabaseEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.DetachDatabase,
            Attachment = attachment
        };
    }

    private StatementStartEvent? HandleStatementStart(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseStatementData(bodyLines, rules, false);
        if (data.Attachment is null || data.Transaction is null || data.StatementId is null)
            return null;

        return new StatementStartEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.ExecuteStatementStart,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            StatementId = data.StatementId.Value,
            Sql = data.Sql ?? string.Empty,
            Parameters = data.Params
        };
    }

    private StatementFinishEvent? HandleStatementFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseStatementData(bodyLines, rules, true);
        if (data.Attachment is null || data.Transaction is null || data.StatementId is null)
            return null;

        return new StatementFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.ExecuteStatementFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            StatementId = data.StatementId.Value,
            Sql = data.Sql ?? string.Empty,
            Parameters = data.Params,
            Performance = data.Performance ?? new PerformanceInfo
            {
                ExecuteMs = 0,
                FetchCount = 0,
                ReadCount = 0,
                WriteCount = 0,
                MarkCount = 0
            },
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }
    
    private FailedStatementFinishEvent? HandleFailedStatementFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseStatementData(bodyLines, rules, true);
        if (data.Attachment is null || data.Transaction is null || data.StatementId is null)
            return null;

        return new FailedStatementFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.FailedExecuteStatementFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            StatementId = data.StatementId.Value,
            Sql = data.Sql ?? string.Empty,
            Parameters = data.Params,
            Performance = data.Performance ?? new PerformanceInfo
            {
                ExecuteMs = 0,
                FetchCount = 0,
                ReadCount = 0,
                WriteCount = 0,
                MarkCount = 0
            },
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }

    private ProcedureStartEvent? HandleProcedureStart(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseProcedureData(bodyLines, rules, false);
        if (data.Attachment is null || data.Transaction is null || data.ProcedureName is null)
            return null;

        return new ProcedureStartEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.ExecuteProcedureStart,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            ProcedureName = data.ProcedureName,
            Parameters = data.Params
        };
    }

    private ProcedureFinishEvent? HandleProcedureFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseProcedureData(bodyLines, rules, true);
        if (data.Attachment is null || data.Transaction is null || data.ProcedureName is null)
            return null;

        return new ProcedureFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.ExecuteProcedureFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            ProcedureName = data.ProcedureName,
            Parameters = data.Params,
            Performance = data.Performance ?? new PerformanceInfo
            {
                ExecuteMs = 0,
                FetchCount = 0,
                ReadCount = 0,
                WriteCount = 0,
                MarkCount = 0
            },
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }
    
    private FailedProcedureFinishEvent? HandleFailedProcedureFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseProcedureData(bodyLines, rules, true);
        if (data.Attachment is null || data.Transaction is null || data.ProcedureName is null)
            return null;

        return new FailedProcedureFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.FailedExecuteProcedureFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            ProcedureName = data.ProcedureName,
            Parameters = data.Params,
            Performance = data.Performance ?? new PerformanceInfo
            {
                ExecuteMs = 0,
                FetchCount = 0,
                ReadCount = 0,
                WriteCount = 0,
                MarkCount = 0
            },
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }

    private TriggerStartEvent? HandleTriggerStart(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseTriggerData(bodyLines, rules, false);
        if (data.Attachment is null || data.Transaction is null ||
            data.TriggerName is null || data.Table is null || data.Timing is null || data.Event is null)
            return null;

        return new TriggerStartEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.ExecuteTriggerStart,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            TriggerName = data.TriggerName,
            Table = data.Table,
            Timing = data.Timing,
            Event = data.Event
        };
    }

    private TriggerFinishEvent? HandleTriggerFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseTriggerData(bodyLines, rules, true);
        if (data.Attachment is null || data.Transaction is null ||
            data.TriggerName is null || data.Table is null || data.Timing is null || data.Event is null)
            return null;

        return new TriggerFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.ExecuteTriggerFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            TriggerName = data.TriggerName,
            Table = data.Table,
            Timing = data.Timing,
            Event = data.Event,
            Performance = data.Performance ?? new PerformanceInfo
            {
                ExecuteMs = 0,
                FetchCount = 0,
                ReadCount = 0,
                WriteCount = 0,
                MarkCount = 0
            },
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }
    
    private FailedTriggerFinishEvent? HandleFailedTriggerFinish(Match header, IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var data = ParseTriggerData(bodyLines, rules, true);
        if (data.Attachment is null || data.Transaction is null ||
            data.TriggerName is null || data.Table is null || data.Timing is null || data.Event is null)
            return null;

        return new FailedTriggerFinishEvent
        {
            Timestamp = ParseTimestamp(header.Groups["ts"].Value),
            TraceId = int.Parse(header.Groups["trace_id"].Value),
            HexTraceId = header.Groups["hex_trace_id"].Value,
            EventType = EventType.FailedExecuteTriggerFinish,
            Attachment = data.Attachment,
            Transaction = data.Transaction,
            TriggerName = data.TriggerName,
            Table = data.Table,
            Timing = data.Timing,
            Event = data.Event,
            Performance = data.Performance ?? new PerformanceInfo
            {
                ExecuteMs = 0,
                FetchCount = 0,
                ReadCount = 0,
                WriteCount = 0,
                MarkCount = 0
            },
            PerformanceTable = _options.ParsePerformanceTables ? data.PerformanceTable : null
        };
    }

    // ==================== Parsing helpers (nested) ====================

    private static TraceSessionInfo? ParseSessionInfo(IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        foreach (var line in lines)
        {
            var m = rules["session"].Match(line);
            if (m.Success)
                return new TraceSessionInfo
                {
                    SessionId = int.Parse(m.Groups["session_id"].Value)
                };
        }

        return null;
    }

    private static AttachmentInfo? ParseAttachmentInfoWithProcess(IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        AttachmentInfo? attachment = null;
        string? procPath = null;
        int? procId = null;

        foreach (var line in lines)
        {
            // attachment
            var am = rules["attachment"].Match(line);
            if (am.Success && attachment is null)
                attachment = new AttachmentInfo
                {
                    DatabasePath = am.Groups["database_path"].Value,
                    AttachmentId = int.Parse(am.Groups["attachment_id"].Value),
                    User = am.Groups["user"].Value,
                    Role = am.Groups["role"].Value,
                    Charset = am.Groups["charset"].Value,
                    Protocol = am.Groups["protocol"].Value.Trim(),
                    Address = am.Groups["address"].Success ? am.Groups["address"].Value : "<internal>",
                    Port = am.Groups["port"].Success ? int.Parse(am.Groups["port"].Value) : 0,
                    ProcessPath = null,
                    ProcessId = null
                };

            // process
            var pm = rules["process"].Match(line);
            if (pm.Success)
            {
                procPath = pm.Groups["process_path"].Value;
                procId = int.Parse(pm.Groups["process_id"].Value);
            }
        }

        if (attachment is null) return null;

        if (procPath is not null || procId is not null)
            attachment = attachment with { ProcessPath = procPath, ProcessId = procId };

        return attachment;
    }

    private static TransactionInfo? ParseTransactionInfo(IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        foreach (var line in lines)
        {
            if (!line.Contains("(TRA_")) continue;

            var m = rules["transaction"].Match(line);
            if (!m.Success) continue;

            var tid = int.Parse(m.Groups[1].Value);
            var rawParams = m.Groups[2].Value;

            // Парсим параметры изоляции (аналог Python logic)
            var parts = rawParams.Split('|')
                .Select(p => p.Trim().ToUpperInvariant())
                .Where(p => !string.IsNullOrEmpty(p) && p != "NONE" && p != "(NONE)")
                .ToList();

            // Заполняем недостающие слоты значениями по умолчанию
            while (parts.Count < 4)
                parts.Add("NONE");

            return new TransactionInfo
            {
                TransactionId = tid,
                IsolationLevel = parts[0],
                ConsistencyMode = parts[1],
                LockMode = parts[2],
                AccessMode = parts[3]
            };
        }

        return null;
    }

    // ==================== Data Records ====================

    private sealed record StatementData(
        AttachmentInfo? Attachment,
        TransactionInfo? Transaction,
        int? StatementId,
        string? Sql,
        IReadOnlyList<SqlParam> Params,
        PerformanceInfo? Performance,
        PerformanceTable? PerformanceTable);

    private sealed record ProcedureData(
        AttachmentInfo? Attachment,
        TransactionInfo? Transaction,
        string? ProcedureName,
        IReadOnlyList<SqlParam> Params,
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

    private static StatementData ParseStatementData(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules,
        bool includePerformance)
    {
        AttachmentInfo? attachment = null;
        TransactionInfo? transaction = null;
        int? statementId = null;
        string? sql = null;
        var paramsList = new List<SqlParam>();
        PerformanceInfo? performance = null;
        PerformanceTable? performanceTable = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // attachment
            if (attachment is null)
            {
                var m = rules["attachment"].Match(line);
                if (m.Success)
                {
                    attachment = new AttachmentInfo
                    {
                        DatabasePath = m.Groups["database_path"].Value,
                        AttachmentId = int.Parse(m.Groups["attachment_id"].Value),
                        User = m.Groups["user"].Value,
                        Role = m.Groups["role"].Value,
                        Charset = m.Groups["charset"].Value,
                        Protocol = m.Groups["protocol"].Value.Trim(),
                        Address = m.Groups["address"].Success ? m.Groups["address"].Value : "<internal>",
                        Port = m.Groups["port"].Success ? int.Parse(m.Groups["port"].Value) : 0,
                        ProcessPath = null,
                        ProcessId = null
                    };
                    continue;
                }
            }

            // process
            if (attachment is not null)
            {
                var pm = rules["process"].Match(line);
                if (pm.Success)
                {
                    attachment = attachment with
                    {
                        ProcessPath = pm.Groups["process_path"].Value,
                        ProcessId = int.Parse(pm.Groups["process_id"].Value)
                    };
                    continue;
                }
            }

            // transaction
            if (transaction is null)
            {
                transaction = ParseTransactionInfo(new[] { line }, rules);
                if (transaction is not null) continue;
            }

            // statement id
            var sm = rules["statement"].Match(line);
            if (sm.Success)
            {
                statementId = int.Parse(sm.Groups["statement_id"].Value);
                continue;
            }

            // SQL block start
            if (line.Trim().StartsWith("-----"))
            {
                var sqlLines = new List<string>();
                i++;
                while (i < lines.Count)
                {
                    var l = lines[i];
                    // break if param or performance/fetched
                    if (rules["parameters"].IsMatch(l) ||
                        (includePerformance && (rules["fetched"].IsMatch(l) || rules["performance"].IsMatch(l))))
                        break;

                    sqlLines.Add(l);
                    i++;
                }

                sql = StringPool.Intern(string.Join("\n", sqlLines).Trim());
                i--; // Корректировка индекса
                continue;
            }

            // parameters
            var paramM = rules["parameters"].Match(line);
            if (paramM.Success)
            {
                var value = paramM.Groups["value"].Success
                    ? paramM.Groups["value"].Value
                    : paramM.Groups["value_null"].Value;

                paramsList.Add(new SqlParam
                {
                    Name = paramM.Groups["name"].Value,
                    Dtype = paramM.Groups["dtype"].Value,
                    Value = value
                });
                continue;
            }

            // performance
            if (includePerformance && performance is null)
            {
                performance = ParsePerformance(new[] { line }, rules);
                if (performance is not null) continue;
            }

            // performance table
            if (includePerformance && performanceTable is null)
            {
                var slice = lines.Skip(i).ToArray();
                performanceTable = PerformanceTableParser.ParsePerformanceTable(slice, rules);
            }
        }
        
        
        return new StatementData(
            Attachment: attachment,
            Transaction: transaction,
            StatementId: statementId,
            Sql: sql,
            Params: paramsList,
            Performance: performance,
            PerformanceTable: performanceTable
        );
    }

    private static ProcedureData ParseProcedureData(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules,
        bool includePerformance)
    {
        AttachmentInfo? attachment = null;
        TransactionInfo? transaction = null;
        string? procedureName = null;
        var paramsList = new List<SqlParam>();
        PerformanceInfo? performance = null;
        PerformanceTable? performanceTable = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (attachment is null)
            {
                var m = rules["attachment"].Match(line);
                if (m.Success)
                {
                    attachment = new AttachmentInfo
                    {
                        DatabasePath = m.Groups["database_path"].Value,
                        AttachmentId = int.Parse(m.Groups["attachment_id"].Value),
                        User = m.Groups["user"].Value,
                        Role = m.Groups["role"].Value,
                        Charset = m.Groups["charset"].Value,
                        Protocol = m.Groups["protocol"].Value.Trim(),
                        Address = m.Groups["address"].Success ? m.Groups["address"].Value : "<internal>",
                        Port = m.Groups["port"].Success ? int.Parse(m.Groups["port"].Value) : 0,
                        ProcessPath = null,
                        ProcessId = null
                    };
                    continue;
                }
            }

            if (attachment is not null)
            {
                var pm = rules["process"].Match(line);
                if (pm.Success)
                {
                    attachment = attachment with
                    {
                        ProcessPath = pm.Groups["process_path"].Value,
                        ProcessId = int.Parse(pm.Groups["process_id"].Value)
                    };
                    continue;
                }
            }

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

            var paramM = rules["parameters"].Match(line);
            if (paramM.Success)
            {
                var value = paramM.Groups["value"].Success
                    ? paramM.Groups["value"].Value
                    : paramM.Groups["value_null"].Value;

                paramsList.Add(new SqlParam
                {
                    Name = paramM.Groups["name"].Value,
                    Dtype = paramM.Groups["dtype"].Value,
                    Value = value
                });
                continue;
            }

            if (includePerformance && performance is null) performance = ParsePerformance(new[] { line }, rules);
        }

        return new ProcedureData(attachment, transaction, procedureName, paramsList, performance, performanceTable);
    }

    private static TriggerData ParseTriggerData(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules,
        bool includePerformance)
    {
        AttachmentInfo? attachment = null;
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

            if (attachment is null)
            {
                var m = rules["attachment"].Match(line);
                if (m.Success)
                {
                    attachment = new AttachmentInfo
                    {
                        DatabasePath = m.Groups["database_path"].Value,
                        AttachmentId = int.Parse(m.Groups["attachment_id"].Value),
                        User = m.Groups["user"].Value,
                        Role = m.Groups["role"].Value,
                        Charset = m.Groups["charset"].Value,
                        Protocol = m.Groups["protocol"].Value.Trim(),
                        Address = m.Groups["address"].Success ? m.Groups["address"].Value : "<internal>",
                        Port = m.Groups["port"].Success ? int.Parse(m.Groups["port"].Value) : 0,
                        ProcessPath = null,
                        ProcessId = null
                    };
                    continue;
                }
            }

            if (attachment is not null)
            {
                var pm = rules["process"].Match(line);
                if (pm.Success)
                {
                    attachment = attachment with
                    {
                        ProcessPath = pm.Groups["process_path"].Value,
                        ProcessId = int.Parse(pm.Groups["process_id"].Value)
                    };
                    continue;
                }
            }

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

            if (includePerformance && performance is null) performance = ParsePerformance(new[] { line }, rules);
        }

        return new TriggerData(attachment, transaction, triggerName, table, timing, eventType, performance,
            performanceTable);
    }

    private static PerformanceInfo? ParsePerformance(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, Regex> rules)
    {
        var fetchCount = 0;

        foreach (var line in lines)
        {
            // Fetch count
            var mFetched = rules["fetched"].Match(line);
            if (mFetched.Success)
                fetchCount = int.Parse(mFetched.Groups["fetch_count"].Value);

            // Metrics
            var mPerf = rules["performance"].Match(line);
            if (mPerf.Success)
                return new PerformanceInfo
                {
                    ExecuteMs = int.Parse(mPerf.Groups["execute_ms"].Value),
                    FetchCount = fetchCount,
                    ReadCount = mPerf.Groups["read"].Success ? int.Parse(mPerf.Groups["read"].Value) : 0,
                    WriteCount = mPerf.Groups["write"].Success ? int.Parse(mPerf.Groups["write"].Value) : 0,
                    MarkCount = mPerf.Groups["mark"].Success ? int.Parse(mPerf.Groups["mark"].Value) : 0
                };
        }

        return null;
    }
}