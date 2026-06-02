namespace FirebirdTraceParser.Parsing.Rules;

internal sealed class RuleConfiguration
{
    public int SchemaVersion { get; set; }
    public Dictionary<string, RuleDefinition> Rules { get; set; } = new();
}