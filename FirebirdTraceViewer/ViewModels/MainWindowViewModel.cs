using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceParser.Core.Models.ValueObjects;
using FirebirdTraceParser.Core.Parsing.Engine;
using FirebirdTraceViewer.Enums;
using FirebirdTraceViewer.Interfaces;
using FirebirdTraceViewer.Models;
using NLog;

namespace FirebirdTraceViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IFileDialogService _fileDialogService;
    private readonly ITraceLogParser _parser;

    // Хранит события по хешу файла для O(1) удаления группы событий
    // HashSet<EventBase> работает через record structural equality — 
    // используем List + индекс для сохранения порядка вставки
    private readonly Dictionary<string, List<EventBase>> _eventsByFileHash = [];

    // Токен для отмены текущей операции загрузки
    private CancellationTokenSource? _loadingCts;

    public ObservableCollection<FileCardViewModel> TraceFileInfos { get; } = [];
    public ObservableCollection<FilterCardModel> FilterCardModels { get; } = [];
    public ObservableCollection<EventBase> Events { get; } = [];
    public StatisticsInfoSectionViewModel StatisticInfoModels { get; }


    /// <summary>
    ///     Выделенные карточки файлов — синхронизируется из code-behind через ListBox.SelectionChanged.
    ///     Хранит ссылки, а не копии — O(1) доступ.
    /// </summary>
    public ObservableCollection<FileCardViewModel> SelectedFileCards { get; } = [];

    [ObservableProperty] public partial SearchType CurrentSearchType { get; set; }

    /// <summary>
    ///     Вычисляемое свойство — нет дублирования состояния.
    ///     Обновляется через OnCurrentSearchTypeChanged.
    /// </summary>
    [ObservableProperty]
    public partial bool IsClassicSearch { get; set; }

    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsFileLoading { get; set; }

    [ObservableProperty] public partial double LoadProgress { get; set; }

    /// <summary>Design-time конструктор — только для XAML превью.</summary>
    public MainWindowViewModel()
    {
        _parser = null!;
        _fileDialogService = null!;
        StatisticInfoModels = new StatisticsInfoSectionViewModel();

        TraceFileInfos.Add(CreateFileCardViewModel(
            new TraceFileInfoModel(
                "2026_05_13__00_01_00.log",
                string.Empty,
                123_456_890,
                new DateTime(2026, 5, 13, 0, 0, 1),
                new DateTime(2026, 5, 13, 0, 20, 0),
                12_345,
                "design-sample-1")));

        FilterCardModels.Add(new FilterCardModel("Пользователь", "BERDIN.A"));
        FilterCardModels.Add(new FilterCardModel("Адрес подключения", "10.0.1.102"));

        Events.Add(new AttachDatabaseEvent
        {
            Attachment = new AttachmentInfo
            {
                Address = "192.168.3.5",
                AttachmentId = 123,
                Charset = "UTF-8",
                DatabasePath = "Path",
                Port = 1010,
                Protocol = "TCP",
                ProcessId = 12,
                ProcessPath = "Process Path",
                User = "BERDIN.A",
                Role = "MON"
            },
            EventType = EventType.AttachDatabase,
            Timestamp = DateTime.Now,
            TraceId = 437_236,
            HexTraceId = "0x7f3133ba1dc0"
        });

        StatusMessage = "Готово (Design Time).";
        IsClassicSearch = true;
    }

    /// <summary>Runtime конструктор — используется DI контейнером.</summary>
    public MainWindowViewModel(IFileDialogService fileDialogService, ITraceLogParser parser)
    {
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));

        StatisticInfoModels = new StatisticsInfoSectionViewModel();

        // Начальные фильтры — в реальном проекте загружаются из настроек
        FilterCardModels.Add(new FilterCardModel("Пользователь", "BERDIN.A"));
        FilterCardModels.Add(new FilterCardModel("Адрес подключения", "10.0.1.102"));

        CurrentSearchType = SearchType.Classic;
        IsClassicSearch = true;
        StatusMessage = "Готов к работе!";

        Logger.Info("MainWindowViewModel инициализирован");
    }

    /// <summary>
    ///     Синхронизирует IsClassicSearch при изменении CurrentSearchType.
    ///     Единственное место управления состоянием поиска.
    /// </summary>
    partial void OnCurrentSearchTypeChanged(SearchType value)
    {
        IsClassicSearch = value == SearchType.Classic;
    }

    [RelayCommand]
    private void SwitchSearchType()
    {
        CurrentSearchType = CurrentSearchType == SearchType.Classic
            ? SearchType.Regexp
            : SearchType.Classic;
        // IsClassicSearch обновится через OnCurrentSearchTypeChanged
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterCardModels.Clear();
        StatusMessage = "Все фильтры очищены.";
    }

    /// <summary>
    ///     Повторно парсит все загруженные файлы последовательно.
    ///     Защищена от запуска во время загрузки.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanReparseFiles))]
    private async Task ReparseAllFilesAsync(CancellationToken cancellationToken)
    {
        if (TraceFileInfos.Count == 0)
        {
            StatusMessage = "Нет загруженных файлов для обработки.";
            return;
        }

        IsFileLoading = true;
        ReparseAllFilesCommand.NotifyCanExecuteChanged();
        ReparseSelectedFilesCommand.NotifyCanExecuteChanged();

        try
        {
            // Снимок коллекции — защита от модификации во время итерации
            var allCards = TraceFileInfos.ToList();

            StatusMessage = $"Повторная обработка всех файлов: 0/{allCards.Count}";

            for (var i = 0; i < allCards.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var card = allCards[i];
                StatusMessage = $"Повторная обработка {i + 1}/{allCards.Count}: {card.FileInfo.FileName}";

                await ReparseTraceFileAsync(card, cancellationToken);
            }

            StatusMessage = $"Все файлы обработаны заново: {allCards.Count} файл(ов).";
            Logger.Info("Повторная обработка всех файлов завершена: {Count}", allCards.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Повторная обработка отменена.";
            Logger.Info("Повторная обработка всех файлов отменена");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при повторной обработке всех файлов");
            StatusMessage = $"Ошибка обработки: {ex.Message}";
        }
        finally
        {
            IsFileLoading = false;
            ReparseAllFilesCommand.NotifyCanExecuteChanged();
            ReparseSelectedFilesCommand.NotifyCanExecuteChanged();
            OpenLocalFileCommand.NotifyCanExecuteChanged();
            UpdateStatistics();
        }
    }

    /// <summary>
    ///     Повторно парсит только выделенные файлы.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanReparseSelectedFiles))]
    private async Task ReparseSelectedFilesAsync(CancellationToken cancellationToken)
    {
        if (SelectedFileCards.Count == 0)
        {
            StatusMessage = "Нет выделенных файлов для обработки.";
            return;
        }

        IsFileLoading = true;
        ReparseAllFilesCommand.NotifyCanExecuteChanged();
        ReparseSelectedFilesCommand.NotifyCanExecuteChanged();

        try
        {
            // Снимок — SelectedFileCards может меняться во время итерации
            var selectedCards = SelectedFileCards.ToList();

            StatusMessage = $"Повторная обработка выделенных файлов: 0/{selectedCards.Count}";

            for (var i = 0; i < selectedCards.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var card = selectedCards[i];
                StatusMessage = $"Повторная обработка {i + 1}/{selectedCards.Count}: {card.FileInfo.FileName}";

                await ReparseTraceFileAsync(card, cancellationToken);
            }

            StatusMessage = $"Выделенные файлы обработаны заново: {selectedCards.Count} файл(ов).";
            Logger.Info("Повторная обработка выделенных файлов завершена: {Count}", selectedCards.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Повторная обработка отменена.";
            Logger.Info("Повторная обработка выделенных файлов отменена");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при повторной обработке выделенных файлов");
            StatusMessage = $"Ошибка обработки: {ex.Message}";
        }
        finally
        {
            IsFileLoading = false;
            
            OnIsFileLoadingChanged(true);
            UpdateStatistics();
        }
    }

    private bool CanReparseFiles()
    {
        return !IsFileLoading && TraceFileInfos.Count > 0;
    }

    private bool CanReparseSelectedFiles()
    {
        return !IsFileLoading && SelectedFileCards.Count > 0;
    }

    /// <summary>
    ///     Открывает диалог выбора файлов и запускает парсинг.
    ///     Защищена от параллельного вызова через CanExecute.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenLocalFileAsync(CancellationToken cancellationToken)
    {
        // Создаём связанный CTS: отмена через кнопку ИЛИ через cancellationToken фреймворка
        _loadingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _loadingCts.Token;

        // Блокируем повторный вызов
        IsFileLoading = true;
        OpenLocalFileCommand.NotifyCanExecuteChanged();

        IReadOnlyList<IStorageFile>? files = null;

        try
        {
            files = await _fileDialogService.OpenTraceFilesAsync();

            if (files == null || files.Count == 0)
            {
                StatusMessage = "Файлы не выбраны.";
                return;
            }

            await ProcessSelectedFilesAsync(files, token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Загрузка отменена.";
            Logger.Info("Загрузка файлов отменена пользователем");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при загрузке файлов");
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsFileLoading = false;
            LoadProgress = 0;
            _loadingCts.Dispose();
            _loadingCts = null;

            // Разблокируем кнопку
            OpenLocalFileCommand.NotifyCanExecuteChanged();
            UpdateStatistics();
        }
    }

    private bool CanOpenFile()
    {
        return !IsFileLoading;
    }

    /// <summary>
    ///     Отменяет текущую загрузку файлов.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelLoading))]
    private void CancelLoading()
    {
        _loadingCts?.Cancel();
    }

    private bool CanCancelLoading()
    {
        return IsFileLoading && _loadingCts != null;
    }
    
    /// <summary>
    /// При изменении IsFileLoading уведомляем все зависимые команды.
    /// Централизованно — не нужно дублировать NotifyCanExecuteChanged везде.
    /// </summary>
    partial void OnIsFileLoadingChanged(bool value)
    {
        OpenLocalFileCommand.NotifyCanExecuteChanged();
        CancelLoadingCommand.NotifyCanExecuteChanged();
        ReparseAllFilesCommand.NotifyCanExecuteChanged();
        ReparseSelectedFilesCommand.NotifyCanExecuteChanged();
    }


    /// <summary>
    ///     Обрабатывает список выбранных файлов последовательно.
    /// </summary>
    private async Task ProcessSelectedFilesAsync(
        IReadOnlyList<IStorageFile> files,
        CancellationToken cancellationToken)
    {
        var addedCount = 0;
        var duplicateCount = 0;
        LoadProgress = 0;

        for (var i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = files[i];
            var path = file.Path.LocalPath;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Logger.Warn("Файл не найден или путь пустой: {Path}", path);
                continue;
            }

            StatusMessage = $"Обработка файла {i + 1}/{files.Count}: {Path.GetFileName(path)}";

            var fileHash = await CalculateFileHashAsync(path, cancellationToken);

            if (IsDuplicate(fileHash))
            {
                duplicateCount++;
                Logger.Warn("Дубликат файла пропущен: {FilePath}", path);
                continue;
            }

            var fileInfo = new FileInfo(path);
            var traceModel = await ParseFileAsync(fileInfo, fileHash, cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
                TraceFileInfos.Add(CreateFileCardViewModel(traceModel)));

            addedCount++;
        }

        StatusMessage = BuildFileAddingStatusMessage(addedCount, duplicateCount);
    }

    /// <summary>
    ///     Парсит один файл, сохраняет события в словарь и добавляет их в коллекцию.
    /// </summary>
    private async Task<TraceFileInfoModel> ParseFileAsync(
        FileInfo fileInfo,
        string fileHash,
        CancellationToken cancellationToken)
    {
        StatusMessage = $"Парсинг: {fileInfo.Name}";
        Logger.Info("Начало парсинга: {FileName}", fileInfo.Name);

        var progress = new Progress<double>(p =>
            LoadProgress = p * 100);

        var parseResult = await _parser.ParseFileAsync(
            fileInfo.FullName,
            progress,
            cancellationToken);

        var eventList = parseResult.Events.ToList();
        _eventsByFileHash[fileHash] = eventList;

        // Добавляем события на UI-потоке одним батчем
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var evt in eventList)
                Events.Add(evt);
        });

        Logger.Info("Файл распарсен: {FileName}, событий: {Count}",
            fileInfo.Name, eventList.Count);

        return new TraceFileInfoModel(
            fileInfo.Name,
            fileInfo.FullName,
            fileInfo.Length,
            eventList.FirstOrDefault()?.Timestamp ?? DateTime.MinValue,
            eventList.LastOrDefault()?.Timestamp ?? DateTime.MinValue,
            eventList.Count,
            fileHash);
    }

    /// <summary>
    ///     Удаляет события файла из коллекции.
    ///     Использует HashSet для O(1) lookup
    /// </summary>
    private void RemoveFileEvents(string fileHash)
    {
        if (!_eventsByFileHash.TryGetValue(fileHash, out var eventsToRemove))
            return;

        // Строим HashSet для O(1) contains-check при фильтрации
        // record EventBase использует structural equality — это корректно
        var eventsSet = eventsToRemove.ToHashSet();

        // Удаляем с конца для минимизации сдвигов в коллекции
        for (var i = Events.Count - 1; i >= 0; i--)
            if (eventsSet.Contains(Events[i]))
                Events.RemoveAt(i);

        _eventsByFileHash.Remove(fileHash);
    }

    /// <summary>Callback для FileCardViewModel — удаление файла.</summary>
    private Task RemoveTraceFileAsync(FileCardViewModel card)
    {
        RemoveFileEvents(card.FileInfo.FileHash);
        TraceFileInfos.Remove(card);
        UpdateStatistics();
        StatusMessage = $"Файл '{card.FileInfo.FileName}' удалён.";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Повторно парсит один файл.
    /// Используется и как callback из FileCardViewModel, и из команд меню.
    /// </summary>
    private async Task ReparseTraceFileAsync(
        FileCardViewModel card,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileInfo = new FileInfo(card.FileInfo.FilePath);

            if (!fileInfo.Exists)
            {
                StatusMessage = $"Файл '{card.FileInfo.FileName}' не найден на диске.";
                Logger.Warn("Файл для повторного парсинга не существует: {Path}", card.FileInfo.FilePath);
                return;
            }

            RemoveFileEvents(card.FileInfo.FileHash);

            var updatedModel = await ParseFileAsync(fileInfo, card.FileInfo.FileHash, cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
                card.FileInfo = updatedModel);
        }
        catch (OperationCanceledException)
        {
            // Пробрасываем выше — обработка в вызывающей команде
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка повторного парсинга: {FileName}", card.FileInfo.FileName);
            StatusMessage = $"Ошибка перепарсинга '{card.FileInfo.FileName}': {ex.Message}";
        }
    }

    private bool IsDuplicate(string fileHash)
    {
        return TraceFileInfos.Any(f =>
            string.Equals(f.FileInfo.FileHash, fileHash, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Создаёт ViewModel для карточки файла с привязкой callbacks.
    /// </summary>
    private FileCardViewModel CreateFileCardViewModel(TraceFileInfoModel fileInfo)
    {
        return new FileCardViewModel(
            fileInfo,
            RemoveTraceFileAsync,
            // Лямбда обёртка для совместимости сигнатур:
            // FileCardViewModel ожидает Func<FileCardViewModel, Task>
            // ReparseTraceFileAsync имеет параметр CancellationToken = default
            card => ReparseTraceFileAsync(card, CancellationToken.None)
        );
    }

    private static async Task<string> CalculateFileHashAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            true);

        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private void UpdateStatistics()
    {
        var totalEvents = TraceFileInfos.Sum(f => f.FileInfo.EventCount);

        StatisticInfoModels.UpdateStatistics([
            new StatisticInfoModel("Файлов:", TraceFileInfos.Count.ToString()),
            new StatisticInfoModel("Всего событий:", totalEvents.ToString("N0")),

            // TODO: заменить на реальное количество после реализации фильтрации
            new StatisticInfoModel("Отфильтрованных событий:", totalEvents.ToString("N0")),
            new StatisticInfoModel("Время парсинга:", "0 сек")
        ]);
    }

    private static string BuildFileAddingStatusMessage(int addedCount, int duplicateCount)
    {
        return (addedCount, duplicateCount) switch
        {
            (> 0, > 0) => $"Добавлено: {addedCount} файл(ов). Пропущено дубликатов: {duplicateCount}.",
            (> 0, 0) => $"Добавлено: {addedCount} файл(ов).",
            (0, > 0) => "Файлы не добавлены: все выбранные файлы уже загружены.",
            _ => "Файлы не выбраны."
        };
    }
}