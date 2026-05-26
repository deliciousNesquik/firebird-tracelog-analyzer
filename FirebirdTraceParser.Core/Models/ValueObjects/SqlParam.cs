using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Описание одного параметра SQL‑запроса.
/// Соответствует Python SqlParam.
/// </summary>
public sealed record SqlParam
{
    private string _name = string.Empty;

    /// <summary>Имя параметра (например, param0, param1)</summary>
    public required string Name
    {
        get => _name;
        init => _name = StringPool.Intern(value);
    }
    
    
    private string _dtype = string.Empty;

    /// <summary>Тип параметра в терминах Firebird (bigint, varchar, etc.)</summary>
    public required string Dtype
    {
        get => _dtype;
        init => _dtype = StringPool.Intern(value);
    }

    private string _value = string.Empty;

    /// <summary>Значение параметра в строковом представлении</summary>
    public required string Value
    {
        get => _value;
        init => _value = StringPool.Intern(value);
    }
}