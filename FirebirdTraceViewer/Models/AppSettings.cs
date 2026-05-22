using Microsoft.Extensions.Configuration;

namespace FirebirdTraceViewer.Models;

/// <summary>
/// Основные настройки приложения
/// </summary>
public class AppSettings
{
    
}

/// <summary>
/// Настройки видимости секций UI
/// </summary>
public sealed class UiSectionSettings
{
    public bool Files { get; set; } = true;
    public bool Search { get; set; } = true;
    public bool Events { get; set; } = true;
    public bool Statistics { get; set; } = true;
    public bool Logs { get; set; } = false;
}