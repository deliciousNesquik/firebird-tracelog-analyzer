using FirebirdTraceParser.Core.Models.Enums;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовая модель события трассировки Firebird.
/// Соответствует Python EventBase.
/// </summary>
public abstract record EventBase
{
    /// <summary>Время события в формате ISO 8601</summary>
    public required DateTime Timestamp { get; init; }
    
    /// <summary>Идентификатор trace‑сессии (decimal)</summary>
    public required int TraceId { get; init; }
    
    /// <summary>Идентификатор trace‑сессии (hexadecimal)</summary>
    public required string HexTraceId { get; init; }
    
    /// <summary>Тип события трассировки</summary>
    public required EventType EventType { get; init; }
}