using FirebirdTraceParser.Core.Models.Events;

namespace FirebirdTraceViewer.Services.Filtering;

public interface IFilteringService
{
    /// <summary>
    /// Получает все доступные фильтры для коллекции событий.
    /// </summary>
    IReadOnlyList<FilterDescriptor> GetAvailableFilters(IEnumerable<EventBase> events);
    
    /// <summary>
    /// Применяет все активные фильтры к коллекции.
    /// </summary>
    IEnumerable<EventBase> ApplyFilters(IEnumerable<EventBase> events, IEnumerable<FilterDescriptor> filters);
    
    /// <summary>
    /// Регистрирует пользовательский фильтр.
    /// </summary>
    void RegisterCustomFilter(FilterDescriptor descriptor);
}