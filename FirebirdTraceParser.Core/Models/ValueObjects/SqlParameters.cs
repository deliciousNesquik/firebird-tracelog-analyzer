using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Описание одного параметра SQL‑запроса.
/// </summary>
public sealed record SqlParameters
{

    /// <summary>Имя параметра (например, param0, param1)</summary>
    public required string Name
    {
        get => field;
        init => field = StringPool.Intern(value);
    }

    /// <summary>Тип параметра в терминах Firebird (bigint, varchar, etc.)</summary>
    public required string Dtype
    {
        get => field;
        init => field = StringPool.Intern(value);
    }

    /// <summary>Значение параметра в строковом представлении</summary>
    public required string Value
    {
        get => field;
        init => field = StringPool.Intern(value);
    }
}