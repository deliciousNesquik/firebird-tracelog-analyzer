namespace FirebirdTraceAnalyzer.Models;

/// <summary>
/// Основные настройки приложения
/// </summary>
public class AppSettings
{
    public bool IsClassicSearch { get; set; }
    public string Theme { get; set; } = "Light";
}

/// <summary>
/// Настройки видимости секций UI
/// </summary>
public class UiSectionSettings
{
    public bool Files { get; set; }
    public bool Search { get; set; }
    public bool Events { get; set; }
    public bool Statistics { get; set; }
    public bool Logs { get; set; }
}