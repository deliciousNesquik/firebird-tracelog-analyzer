namespace firebird_tracelog_viewer.Models;

public class FilterCardModel(string name, string value)
{
    public string FilterName { get; set; } = name;
    public string FilterValue { get; set; } = value;
}