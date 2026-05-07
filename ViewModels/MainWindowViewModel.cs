using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FTV.Enums;
using FTV.Models;

namespace FTV.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<TraceFileInfoModel> TraceFileInfos { get; set; }
    public ObservableCollection<FilterCardModel> FilterCardModels { get; set; }
    public ObservableCollection<StatisticInfoModel> StatisticInfoModels { get; set; }

    [ObservableProperty] public partial SearchType CurrentSearchType { get; set; }
    [ObservableProperty] public partial bool IsClassicSearch { get; set; }

    [RelayCommand]
    public void SwitchSearchType()
    {
        CurrentSearchType = CurrentSearchType == SearchType.Classic ? SearchType.Regexp : SearchType.Classic;
    }

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

        StatisticInfoModels =
        [
            new StatisticInfoModel("Файлов:", TraceFileInfos.Count.ToString()),
            new StatisticInfoModel("Всего событий", TraceFileInfos.Sum(p => p.EventCount).ToString("N0")),
            new StatisticInfoModel("Отфильтрованных событий", TraceFileInfos.Sum(p => p.EventCount).ToString("N0")),
            new StatisticInfoModel("Время парсинга", "2мин 32сек")
        ];
        
        CurrentSearchType = SearchType.Classic;
        IsClassicSearch = CurrentSearchType == SearchType.Classic;
    }
}