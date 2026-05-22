using FirebirdTraceViewer.Models;

namespace FirebirdTraceViewer.Mocks;

public class AppSettingsMock: AppSettings
{
    public new bool IsClassicSearch { get; init; } = true;
}

public class UiSectionSettingsMock: UiSectionSettings
{
    public new bool Files { get; init; } = true;
    public new bool Search { get; init; } = true;
    public new bool Events { get; init; } = true;
    public new bool Statistics { get; init; } = true;
    public new bool Logs { get; init; } = true;
}