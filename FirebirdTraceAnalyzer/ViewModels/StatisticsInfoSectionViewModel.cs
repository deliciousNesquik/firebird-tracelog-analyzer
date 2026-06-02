using System.Collections.ObjectModel;
using FirebirdTraceAnalyzer.Models;

namespace FirebirdTraceAnalyzer.ViewModels;

public partial class StatisticsInfoSectionViewModel : ViewModelBase
{
    public ObservableCollection<StatisticInfoModel> StatisticInfoModels { get; } = [];

    public void UpdateStatistics(IReadOnlyList<StatisticInfoModel> statistics)
    {
        // Точечное обновление вместо Clear+AddRange — меньше UI-нотификаций
        for (var i = 0; i < statistics.Count; i++)
        {
            if (i < StatisticInfoModels.Count)
                StatisticInfoModels[i] = statistics[i];
            else
                StatisticInfoModels.Add(statistics[i]);
        }

        // Удаляем лишние если список стал короче
        while (StatisticInfoModels.Count > statistics.Count)
            StatisticInfoModels.RemoveAt(StatisticInfoModels.Count - 1);
    }
}