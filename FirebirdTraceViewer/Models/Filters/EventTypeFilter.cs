using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Interfaces;

namespace FirebirdTraceViewer.Models.Filters;

/// <summary>
/// Фильтр по типам событий (AttachDatabase, ExecuteStatement и т.д.)
/// Поддерживает выбор одного или нескольких типов.
/// </summary>
public class EventTypeFilter : IFilter
{
    // Кэш для быстрого поиска O(1)
    private HashSet<EventType> _selectedTypes = [];

    public string Name => "Тип события";

    /// <summary>Выбранные типы событий</summary>
    public IReadOnlyCollection<EventType> SelectedTypes => _selectedTypes;

    /// <summary>Все доступные типы событий</summary>
    public static IReadOnlyList<EventType> AvailableTypes { get; } = 
        Enum.GetValues<EventType>().OrderBy(e => e.ToString()).ToList();

    /// <summary>Проверяет, выбран ли конкретный тип</summary>
    public bool IsSelected(EventType eventType) => _selectedTypes.Contains(eventType);

    /// <summary>Добавляет тип в фильтр</summary>
    public void SelectType(EventType eventType) => _selectedTypes.Add(eventType);

    /// <summary>Удаляет тип из фильтра</summary>
    public void DeselectType(EventType eventType) => _selectedTypes.Remove(eventType);

    /// <summary>Выбирает все типы</summary>
    public void SelectAll() => _selectedTypes = new HashSet<EventType>(AvailableTypes);

    /// <summary>Проверяет, проходит ли событие фильтр</summary>
    public bool Matches(EventBase @event)
    {
        // Если ничего не выбрано — все события проходят
        if (_selectedTypes.Count == 0)
            return true;

        return _selectedTypes.Contains(@event.EventType);
    }

    public void Reset()
    {
        _selectedTypes.Clear();
    }
}