using FirebirdTraceParser.Core.Models.Events;

namespace FirebirdTraceViewer.Interfaces;

/// <summary>
/// Интерфейс для фильтра.
/// Каждый фильтр может проверять разные критерии (тип события, пользователь, время и т.д.)
/// </summary>
public interface IFilter
{
    /// <summary>Название фильтра для UI</summary>
    string Name { get; }

    /// <summary>Возвращает true, если событие проходит фильтр</summary>
    bool Matches(EventBase @event);

    /// <summary>Сбрасывает фильтр в исходное состояние (ничего не фильтрует)</summary>
    void Reset();
}