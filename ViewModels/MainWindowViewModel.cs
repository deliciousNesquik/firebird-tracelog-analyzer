using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia.Controls.Converters;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FTV.Enums;
using FTV.Interfaces;
using FTV.Models;

namespace FTV.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;

    public ObservableCollection<FileCardViewModel> TraceFileInfos { get; set; } = [];
    public ObservableCollection<FilterCardModel> FilterCardModels { get; set; }
    public ObservableCollection<StatisticInfoModel> StatisticInfoModels { get; set; }

    [ObservableProperty] public partial SearchType CurrentSearchType { get; set; }
    [ObservableProperty] public partial bool IsClassicSearch { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsFileLoading { get; set; }

    [RelayCommand]
    public void SwitchSearchType()
    {
        CurrentSearchType = CurrentSearchType == SearchType.Classic ? SearchType.Regexp : SearchType.Classic;
    }

    [RelayCommand]
    public void ClearFilters()
    {
        // TODO: Вызвать метод для обновления списка событий без фильтров
        FilterCardModels.Clear();
    }

    [RelayCommand]
    public void AddFilter()
    {
        try
        {
            var random = new Random();

            // Случайный ключ и значение
            var keys = new[] { "Пользователь", "Адрес", "Тип события", "Время события" };
            var key = keys[random.Next(keys.Length)];

            string value;
            switch (key)
            {
                case "Пользователь":
                    value = new[] { "BERDIN.A", "IVANOV.B", "PETROV.C" }[random.Next(3)];
                    break;
                case "Адрес":
                    value = $"{random.Next(1, 255)}.0.0.{random.Next(1, 255)}";
                    break;
                case "Тип события":
                    value = new[] { "AttachDatabase", "DetachDatabase", "Login" }[random.Next(3)];
                    break;
                case "Время события":
                    var now = DateTime.Now;
                    value = now.AddHours(random.Next(-24, 25)).ToString("yyyy-MM-ddTHH:mm:ss.ffff");
                    break;
                default:
                    value = "Unknown";
                    break;
            }

            FilterCardModels.Add(new FilterCardModel(key, value)); // Один элемент: название + значение
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    [RelayCommand]
    private async Task OpenLocalFileAsync()
    {
        var files = await _fileDialogService.OpenTraceFilesAsync();
        var addedCount = 0;
        var duplicateCount = 0;

        IsFileLoading = true;
        StatusMessage = "Вычисление хеш-сумм выбранных файлов...";

        try
        {
            foreach (var file in files)
            {
                var path = file.Path.LocalPath;

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    continue;
                }

                var fileInfo = new FileInfo(path);
                var fileHash = await CalculateFileHashAsync(path);

                if (TraceFileInfos.Any(info => string.Equals(info.FileInfo.FileHash, fileHash, StringComparison.OrdinalIgnoreCase)))
                {
                    duplicateCount++;
                    continue;
                }

                TraceFileInfos.Add(
                    CreateFileCardViewModel(
                        new TraceFileInfoModel(
                            fileName: fileInfo.Name,
                            filePath: fileInfo.FullName,
                            fileSize: fileInfo.Length,
                            eventCount: 0,
                            startTrace: new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 1, second: 0),
                            endTrace: new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 20, second: 0),
                            fileHash: fileHash
                        )
                    )
                );

                addedCount++;
            }
        }
        finally
        {
            IsFileLoading = false;
            UpdateStatistics();
            StatusMessage = BuildFileAddingStatusMessage(addedCount, duplicateCount);
        }
    }

    private static async Task<string> CalculateFileHashAsync(string filePath)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            useAsync: true);

        var hashBytes = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    private FileCardViewModel CreateFileCardViewModel(TraceFileInfoModel fileInfo)
    {
        return new FileCardViewModel(fileInfo, RemoveTraceFile);
    }

    private void RemoveTraceFile(FileCardViewModel fileCardViewModel)
    {
        TraceFileInfos.Remove(fileCardViewModel);
        UpdateStatistics();
        StatusMessage = $"Файл '{fileCardViewModel.FileInfo.FileName}' удалён.";
    }

    private static string BuildFileAddingStatusMessage(int addedCount, int duplicateCount)
    {
        return (addedCount, duplicateCount) switch
        {
            (> 0, > 0) => $"Добавлено файлов: {addedCount}. Пропущено дубликатов: {duplicateCount}.",
            (> 0, 0) => $"Добавлено файлов: {addedCount}.",
            (0, > 0) => $"Файлы не добавлены: выбранные файлы уже есть в списке.",
            _ => "Файлы не выбраны."
        };
    }

    private void UpdateStatistics()
    {
        if (StatisticInfoModels.Count < 3)
        {
            return;
        }

        StatisticInfoModels[0].Value = TraceFileInfos.Count.ToString();
        StatisticInfoModels[1].Value = TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0");
        StatisticInfoModels[2].Value = TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0");
    }

    public MainWindowViewModel()
    {
        _fileDialogService = null!;
        TraceFileInfos =
        [
            CreateFileCardViewModel(new TraceFileInfoModel("2026_05_13__00_01_00.log", string.Empty, 123456890, new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 0, second: 1), new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 20, second: 0), 123456, "design-sample-1")),
            CreateFileCardViewModel(new TraceFileInfoModel("2026_05_13__20_01_00.log", string.Empty, 123456890, new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 20, second: 1), new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 40, second: 0), 123456, "design-sample-2")),
            CreateFileCardViewModel(new TraceFileInfoModel("2026_05_13__40_01_00.log", string.Empty, 123456890, new DateTime(year: 2026, month: 5, day: 13, hour: 0, minute: 40, second: 1), new DateTime(year: 2026, month: 5, day: 13, hour: 1, minute: 0, second: 0), 123456, "design-sample-3")),
        ];

        FilterCardModels =
        [
            new FilterCardModel("Пользователь", "BERDIN.A"),
            new FilterCardModel("Адрес подключения", "10.0.1.102")
        ];

        StatisticInfoModels = CreateStatisticInfoModels();
        StatusMessage = "Готово.";
    }

    public MainWindowViewModel(IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService;

        FilterCardModels =
        [
            new FilterCardModel("Пользователь", "BERDIN.A"),
            new FilterCardModel("Адрес подключения", "10.0.1.102")
        ];

        StatisticInfoModels = CreateStatisticInfoModels();

        CurrentSearchType = SearchType.Classic;
        IsClassicSearch = CurrentSearchType == SearchType.Classic;
        StatusMessage = "Готово.";
    }

    private ObservableCollection<StatisticInfoModel> CreateStatisticInfoModels()
    {
        return
        [
            new StatisticInfoModel("Файлов:", TraceFileInfos.Count.ToString()),
            new StatisticInfoModel("Всего событий", TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0")),
            new StatisticInfoModel("Отфильтрованных событий", TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0")),
            new StatisticInfoModel("Время парсинга", "2мин 32сек")
        ];
    }
}
