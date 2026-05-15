using FirebirdTraceParser.Core.Parsing.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdTraceViewer.Interfaces;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceViewer.Models;
using FirebirdTraceViewer.Enums;
using Avalonia.Threading; 
using NLog;

namespace FirebirdTraceViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // логгер для регистрации всех действий и событий NLog
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // сервис для открытия файлов локально
    private readonly IFileDialogService _fileDialogService;

    // сервис парсер, выполняющий обработку файлов трейс логов
    private readonly ITraceLogParser _parser;

    // коллекция трейс файлов для отображений в виде карточек
    public ObservableCollection<FileCardViewModel> TraceFileInfos { get; set; } = [];

    // коллекция фильтров для отображения в виде карточек
    public ObservableCollection<FilterCardModel> FilterCardModels { get; set; }

    // коллекция статистических данных для отображения в интерфейсе
    public ObservableCollection<StatisticInfoModel> StatisticInfoModels { get; set; }
    [ObservableProperty] public partial SearchType CurrentSearchType { get; set; }
    [ObservableProperty] public partial bool IsClassicSearch { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; }
    [ObservableProperty] public partial bool IsFileLoading { get; set; }
    [ObservableProperty] public partial double LoadProgress { get; set; }


    #region DesignTime

    /// <summary>
    ///     Design-time конструктор для XAML превью.
    /// </summary>
    public MainWindowViewModel()
    {
        _parser = null!;
        _fileDialogService = null!;

        // Design-time данные
        TraceFileInfos =
        [
            CreateFileCardViewModel(new TraceFileInfoModel("2026_05_13__00_01_00.log", string.Empty, 123456890,
                new DateTime(2026, 5, 13, 0, 0, 1), new DateTime(2026, 5, 13, 0, 20, 0), 12345, "design-sample-1"))
        ];
        FilterCardModels =
        [
            new FilterCardModel("Пользователь", "BERDIN.A"), new FilterCardModel("Адрес подключения", "10.0.1.102")
        ];
        StatisticInfoModels = CreateStatisticInfoModels();
        StatusMessage = "Готово (Design Time).";
    }

    #endregion
    

    /// <summary>
    ///     Runtime конструктор с DI.
    /// </summary>
    public MainWindowViewModel(IFileDialogService fileDialogService, ITraceLogParser parser)
    {
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        FilterCardModels =
        [
            new FilterCardModel("Пользователь", "BERDIN.A"), new FilterCardModel("Адрес подключения", "10.0.1.102")
        ];
        StatisticInfoModels = CreateStatisticInfoModels();
        CurrentSearchType = SearchType.Classic;
        IsClassicSearch = CurrentSearchType == SearchType.Classic;
        StatusMessage = "Готово.";
        Logger.Info("MainWindowViewModel инициализирован");
    }

    // ========== Команды ==========

    [RelayCommand]
    public void SwitchSearchType()
    {
        CurrentSearchType = CurrentSearchType == SearchType.Classic ? SearchType.Regexp : SearchType.Classic;
        IsClassicSearch = CurrentSearchType == SearchType.Classic;
    }

    [RelayCommand]
    public void ClearFilters()
    {
        FilterCardModels.Clear();
        StatusMessage = "Фильтры очищены.";
    }

    [RelayCommand]
    public void AddFilter()
    {
        try
        {
            var random = new Random();
            var keys = new[] { "Пользователь", "Адрес", "Тип события", "Время события" };
            var key = keys[random.Next(keys.Length)];
            var value = key switch
            {
                "Пользователь" => new[] { "BERDIN.A", "IVANOV.B", "PETROV.C" }[random.Next(3)],
                "Адрес" => $"{random.Next(1, 255)}.0.0.{random.Next(1, 255)}",
                "Тип события" => new[] { "AttachDatabase", "DetachDatabase", "Login" }[random.Next(3)],
                "Время события" => DateTime.Now.AddHours(random.Next(-24, 25)).ToString("yyyy-MM-ddTHH:mm:ss.ffff"),
                _ => "Unknown"
            };
            FilterCardModels.Add(new FilterCardModel(key, value));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при добавлении фильтра");
        }
    }

    [RelayCommand]
    private async Task OpenLocalFileAsync()
    {
        var files = await _fileDialogService.OpenTraceFilesAsync();
        if (files == null || !files.Any())
        {
            StatusMessage = "Файлы не выбраны.";
            return;
        }

        var addedCount = 0;
        var duplicateCount = 0;
        IsFileLoading = true;
        LoadProgress = 0;
        try
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var path = file.Path.LocalPath;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                StatusMessage = $"Обработка файла {i + 1}/{files.Count}: {Path.GetFileName(path)}";
                var fileInfo = new FileInfo(path);
                var fileHash = await CalculateFileHashAsync(path);

                // Проверка дубликатов
                if (TraceFileInfos.Any(info =>
                        string.Equals(info.FileInfo.FileHash, fileHash, StringComparison.OrdinalIgnoreCase)))
                {
                    duplicateCount++;
                    Logger.Warn("Дубликат файла пропущен: {FilePath}", path);
                    continue;
                }

                // ========== ПАРСИНГ ФАЙЛА ==========
                StatusMessage = $"Парсинг файла: {fileInfo.Name}";
                LoadProgress = 0;
                var progress = new Progress<double>(p => LoadProgress = p * 100);
                var parseResult = await _parser.ParseFileAsync(path, progress);
                Logger.Info("Файл распарсен: {FileName}, события: {EventCount}", fileInfo.Name,
                    parseResult.Events.Count);

                // Определение временных границ
                var startTrace = parseResult.Events.FirstOrDefault()?.Timestamp ?? DateTime.MinValue;
                var endTrace = parseResult.Events.LastOrDefault()?.Timestamp ?? DateTime.MinValue;
                TraceFileInfos.Add(CreateFileCardViewModel(new TraceFileInfoModel(fileInfo.Name, fileInfo.FullName,
                    fileInfo.Length, eventCount: parseResult.Events.Count, startTrace: startTrace, endTrace: endTrace,
                    fileHash: fileHash)));
                addedCount++;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при загрузке файлов");
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsFileLoading = false;
            LoadProgress = 0;
            UpdateStatistics();
            StatusMessage = BuildFileAddingStatusMessage(addedCount, duplicateCount);
        }
    }

    // ========== Helper Methods ==========

    private static async Task<string> CalculateFileHashAsync(string filePath)
    {
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
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
            (0, > 0) => "Файлы не добавлены: выбранные файлы уже есть в списке.",
            _ => "Файлы не выбраны."
        };
    }

    private async void UpdateStatistics()
    {
        StatisticInfoModels.Clear();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatisticInfoModels.Add(new StatisticInfoModel("Файлов:", TraceFileInfos.Count.ToString()));
            StatisticInfoModels.Add(new StatisticInfoModel("Всего событий",
                TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0")));
            StatisticInfoModels.Add(new StatisticInfoModel("Отфильтрованных событий",
                TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0")));
            StatisticInfoModels.Add(new StatisticInfoModel("Время парсинга", "0сек"));
        });
    }

    private ObservableCollection<StatisticInfoModel> CreateStatisticInfoModels()
    {
        return
        [
            new StatisticInfoModel("Файлов:", TraceFileInfos.Count.ToString()),
            new StatisticInfoModel("Всего событий", TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0")),
            new StatisticInfoModel("Отфильтрованных событий",
                TraceFileInfos.Sum(p => p.FileInfo.EventCount).ToString("N0")),
            new StatisticInfoModel("Время парсинга", "0сек")
        ];
    }
}