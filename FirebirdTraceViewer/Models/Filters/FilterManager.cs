using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Interfaces;

namespace FirebirdTraceViewer.Models.Filters;

/// <summary>
/// Управляет всеми активными фильтрами.
/// Комбинирует результаты всех фильтров (AND логика).
/// </summary>
public class FilterManager
{
    private readonly Dictionary<string, IFilter> _filters = [];

    /// <summary>Все зарегистрированные фильтры</summary>
    public IReadOnlyDictionary<string, IFilter> Filters => _filters;

    /// <summary>Регистрирует фильтр</summary>
    public void RegisterFilter(IFilter filter)
    {
        _filters[filter.Name] = filter;
    }

    /// <summary>
    /// Проверяет, проходит ли событие все активные фильтры (AND логика).
    /// </summary>
    public bool IsEventVisible(EventBase @event)
    {
        // Событие видимо, если проходит ВСЕ фильтры
        return _filters.Values.All(filter => filter.Matches(@event));
    }

    /// <summary>Сбрасывает все фильтры</summary>
    public void ResetAll()
    {
        foreach (var filter in _filters.Values)
            filter.Reset();
    }

    /// <summary>Возвращает конкретный фильтр по названию</summary>
    public T? GetFilter<T>(string name) where T : class, IFilter
    {
        return _filters.TryGetValue(name, out var filter) ? filter as T : null;
    }
}