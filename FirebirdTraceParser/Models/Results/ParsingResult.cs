using FirebirdTraceParser.Core.Models.Events;

namespace FirebirdTraceParser.Core.Models.Results;

/// <summary>
/// Результат парсинга с событиями и предупреждениями.
/// </summary>
/// <typeparam name="T">Тип события (EventBase или производные).</typeparam>
public sealed record ParsingResult<T> where T : EventBase
{
    /// <summary>Успешно распарсенные события</summary>
    public required IReadOnlyList<T> Events { get; init; }
    
    /// <summary>Предупреждения и ошибки парсинга</summary>
    public required IReadOnlyList<ParsingWarning> Warnings { get; init; }
    
    /// <summary>Есть ли критические ошибки</summary>
    public bool HasErrors => Warnings.Any(w => w.Severity == WarningSeverity.Error);
    
    /// <summary>Количество пропущенных блоков</summary>
    public int SkippedBlocks => Warnings.Count(w => w.Severity >= WarningSeverity.Warning);
}