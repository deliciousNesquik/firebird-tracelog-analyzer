using System;
using System.Collections.ObjectModel;
using firebird_tracelog_viewer.Models;

namespace firebird_tracelog_viewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<TraceFileInfoModel> TraceFileInfos { get; set; }
    public ObservableCollection<FilterCardModel> FilterCardModels { get; set; }

    public MainWindowViewModel()
    {
        TraceFileInfos =
        [
            new TraceFileInfoModel(
                "2026_05_07__00_00_01.log",
                184767634,
                new DateTime(year: 2026, month: 5, day: 7, hour: 0, minute: 0, second: 1),
                new DateTime(year: 2026, month: 5, day: 7, hour: 0, minute: 20, second: 0),
                435542),

            new TraceFileInfoModel(
                "2026_05_07__00_20_01.log",
                65987231,
                new DateTime(year: 2026, month: 5, day: 7, hour: 0, minute: 20, second: 1),
                new DateTime(year: 2026, month: 5, day: 7, hour: 0, minute: 40, second: 0),
                325397),
            
            new TraceFileInfoModel(
                "2026_05_07__00_40_01.log",
                25987181,
                new DateTime(year: 2026, month: 5, day: 7, hour: 0, minute: 40, second: 1),
                new DateTime(year: 2026, month: 5, day: 7, hour: 1, minute: 0, second: 0),
                121357)
        ];

        FilterCardModels =
        [
            new FilterCardModel("Пользователь", "BERDIN.A"),
            new FilterCardModel("Адрес подключения", "10.0.1.102")
        ];
    }
}