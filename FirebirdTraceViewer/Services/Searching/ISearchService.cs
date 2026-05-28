using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Enums;

namespace FirebirdTraceViewer.Services.Searching;

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