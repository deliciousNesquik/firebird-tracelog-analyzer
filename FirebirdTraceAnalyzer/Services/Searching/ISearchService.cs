using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceAnalyzer.Enums;

namespace FirebirdTraceAnalyzer.Services.Searching;

public interface ISearchService
{
    /// <summary>
    /// Выполняет поиск по SQL, процедурам и триггерам
    /// </summary>
    IEnumerable<EventBase> Search(
        IEnumerable<EventBase> events, 
        string searchText, 
        SearchType mode);
}