using System.Text.RegularExpressions;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Enums;
using NLog;

namespace FirebirdTraceViewer.Services.Searching;

public sealed class SearchService : ISearchService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public IEnumerable<EventBase> Search(
        IEnumerable<EventBase> events,
        string searchText,
        SearchType mode)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Logger.Debug("Поисковый запрос пуст, возвращаю все события");
            return events;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        IEnumerable<EventBase> results = mode switch
        {
            SearchType.Classic => SearchClassic(events, searchText),
            SearchType.Regex => SearchRegex(events, searchText),
            _ => events
        };

        var resultList = results.ToList();

        Logger.Info("Поиск '{Query}' ({Mode}) завершён за {Elapsed}ms, найдено: {Count}",
            searchText, mode, sw.ElapsedMilliseconds, resultList.Count);

        return resultList;
    }

    private IEnumerable<EventBase> SearchClassic(IEnumerable<EventBase> events, string searchText)
    {
        var query = searchText.Trim();
        var comparison = StringComparison.OrdinalIgnoreCase;

        foreach (var evt in events)
        {
            // Поиск в SQL
            if (evt is StatementEventBase stmtEvt &&
                !string.IsNullOrWhiteSpace(stmtEvt.Sql) &&
                stmtEvt.Sql.Contains(query, comparison))
            {
                yield return evt;
                continue;
            }

            // Поиск в процедурах
            if (evt is ProcedureEventBase procEvt &&
                !string.IsNullOrWhiteSpace(procEvt.ProcedureName) &&
                procEvt.ProcedureName.Contains(query, comparison))
            {
                yield return evt;
                continue;
            }

            // Поиск в триггерах
            if (evt is TriggerEventBase triggerEvt &&
                !string.IsNullOrWhiteSpace(triggerEvt.TriggerName) &&
                triggerEvt.TriggerName.Contains(query, comparison))
            {
                yield return evt;
            }
        }
    }

    private IEnumerable<EventBase> SearchRegex(IEnumerable<EventBase> events, string pattern)
    {
        Regex regex;

        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        }
        catch (ArgumentException ex)
        {
            Logger.Error(ex, "Неверное регулярное выражение: {Pattern}", pattern);
            yield break;
        }

        foreach (var evt in events)
        {
            try
            {
                // Поиск в SQL
                if (evt is StatementEventBase stmtEvt &&
                    !string.IsNullOrWhiteSpace(stmtEvt.Sql) &&
                    regex.IsMatch(stmtEvt.Sql))
                {
                    //yield return evt;
                    continue;
                }

                // Поиск в процедурах
                if (evt is ProcedureEventBase procEvt &&
                    !string.IsNullOrWhiteSpace(procEvt.ProcedureName) &&
                    regex.IsMatch(procEvt.ProcedureName))
                {
                    //yield return evt;
                    continue;
                }

                // Поиск в триггерах
                if (evt is TriggerEventBase triggerEvt &&
                    !string.IsNullOrWhiteSpace(triggerEvt.TriggerName) &&
                    regex.IsMatch(triggerEvt.TriggerName))
                {
                    //yield return evt;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                Logger.Warn("Timeout при поиске regex для события {EventType}", evt.EventType);
            }
        }
    }
}