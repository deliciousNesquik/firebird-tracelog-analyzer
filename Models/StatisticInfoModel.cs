namespace FTV.Models;

public class StatisticInfoModel(string header, string value)
{
    public string Header { get; set; } = header;
    public string Value { get; set; } = value;
}