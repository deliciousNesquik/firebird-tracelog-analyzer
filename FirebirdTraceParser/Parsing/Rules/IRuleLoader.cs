using System.Text.RegularExpressions;

namespace FirebirdTraceParser.Parsing.Rules;

public interface IRuleLoader
{
    IReadOnlyDictionary<string, Regex> LoadRules(string configPath);
}