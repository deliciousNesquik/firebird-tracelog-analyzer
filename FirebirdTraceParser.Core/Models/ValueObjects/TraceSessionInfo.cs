using FirebirdTraceParser.Core.Attributes;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о глобальной сессии трассировки Firebird.
/// Соответствует Python TraceSessionInfo.
/// </summary>
public sealed record TraceSessionInfo
{
    /// <summary>Идентификатор глобальной trace‑сессии</summary>
    [FilterableField("ID сессии", Category = "Общие", FilterType =  FilterType.StringMultiSelect)]
    public required int SessionId { get; init; }
}