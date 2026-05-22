using System.Runtime.CompilerServices;
using FirebirdTraceViewer.Models;

namespace FirebirdTraceViewer.Mocks;

public class AppSettingsMock: AppSettings
{
    public AppSettingsMock()
    {
        IsClassicSearch = true;
        Theme = "Light";
    }
}

public class UiSectionSettingsMock: UiSectionSettings
{
    public UiSectionSettingsMock()
    {
        Files = true;
        Search = true;
        Events = true;
        Statistics = true;
        Logs = true;
    }
}