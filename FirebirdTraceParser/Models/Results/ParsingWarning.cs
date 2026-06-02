using FirebirdTraceParser.Core.Models.Enums;

namespace FirebirdTraceParser.Core.Models.Results;

/// <summary>
/// Предупреждение о проблеме парсинга.
/// </summary>
public sealed record ParsingWarning
{
    public required WarningSeverity Severity { get; init; }
    public required string Message { get; init; }
    public int LineNumber { get; init; }
    public string? BlockContent { get; init; }
    public EventType? EventType { get; init; }
}