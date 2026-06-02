namespace FirebirdTraceAnalyzer.Models;

public class FilterCardModel(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}