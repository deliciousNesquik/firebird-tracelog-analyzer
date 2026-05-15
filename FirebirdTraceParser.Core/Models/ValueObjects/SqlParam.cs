namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Описание одного параметра SQL‑запроса.
/// Соответствует Python SqlParam.
/// </summary>
public sealed record SqlParam
{
    /// <summary>Имя параметра (например, param0, param1)</summary>
    public required string Name { get; init; }
    
    /// <summary>Тип параметра в терминах Firebird (bigint, varchar, etc.)</summary>
    public required string Dtype { get; init; }
    
    /// <summary>Значение параметра в строковом представлении</summary>
    public required string Value { get; init; }
}