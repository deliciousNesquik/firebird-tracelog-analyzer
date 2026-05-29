using System.ComponentModel.DataAnnotations;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Attributes;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Базовая модель события трассировки Firebird.
/// Соответствует Python EventBase.
/// </summary>
public class EventBase
{
    /// <summary>Время события в формате ISO 8601</summary>
    [SortableField("Время события", Priority = 1, Category = "Общие", IsDefault = true)]
    [FilterableField("Время события", Category = "Общие", FilterType = FilterType.DateTimeRange)]
    public required DateTime Timestamp { get; init; }
    
    /// <summary>Идентификатор trace‑сессии (decimal)</summary>
    [SortableField("ID трассировки", Priority = 2, Category = "Общие")]
    [FilterableField("ID трассировки", Category = "Общие", FilterType =  FilterType.StringMultiSelect)]
    public required int TraceId { get; init; }
    
    /// <summary>Идентификатор trace‑сессии (hexadecimal)</summary>
    [SortableField("Hex ID трассировки", Priority = 3, Category = "Общие")]
    [FilterableField("Hex ID трассировки", Category = "Общие", FilterType =  FilterType.StringMultiSelect)]
    public required string HexTraceId { get; init; }
    
    /// <summary>Тип события трассировки</summary>
    [SortableField("Тип события", Priority = 4, Category = "Общие")]
    [FilterableField("Тип события", Category = "Общие", FilterType =  FilterType.EnumMultiSelect)]
    public required EventType EventType { get; init; }
}