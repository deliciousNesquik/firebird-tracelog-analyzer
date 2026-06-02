namespace FirebirdTraceParser.Parsing.Rules;

internal sealed class RuleDefinition
{
    public required string Pattern { get; set; }
    public string[]? Flags { get; set; }
    public string Description { get; set; } = "";
    public string[] RequiredGroups { get; set; } = Array.Empty<string>();
    public string? Sample { get; set; }
}