using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Attributes;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовая модель события трассировки Firebird.
/// Соответствует Python EventBase.
/// </summary>
public abstract record EventBase
{
    /// <summary>Время события в формате ISO 8601</summary>
    [SortableField("Время события", Priority = 1, Category = "Общие")]
    public required DateTime Timestamp { get; init; }
    
    /// <summary>Идентификатор trace‑сессии (decimal)</summary>
    [SortableField("ID трассировки", Priority = 2, Category = "Общие")]
    public required int TraceId { get; init; }
    
    /// <summary>Идентификатор trace‑сессии (hexadecimal)</summary>
    [SortableField("Hex ID трассировки", Priority = 3, Category = "Общие")]
    public required string HexTraceId { get; init; }
    
    /// <summary>Тип события трассировки</summary>
    [SortableField("Тип события", Priority = 4, Category = "Общие")]
    public required EventType EventType { get; init; }
}