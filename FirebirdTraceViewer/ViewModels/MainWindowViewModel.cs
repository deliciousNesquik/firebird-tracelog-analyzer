using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia;
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
using FirebirdTraceViewer.Models.Filters;
using FirebirdTraceViewer.Services.Sorting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NLog;

namespace FirebirdTraceViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly AppSettings _appSettings;
    private readonly UiSectionSettings _uiSettings;
    
    // Сервис для открытия файлов через стандартный проводник.
    private readonly IFileDialogService _fileDialogService;

    // Сервис парсера для обработки потока данных файла.
    private readonly ITraceLogParser _parser;

    private readonly ISortingService _sortingService;
    public ObservableCollection<SortDescriptor> AvailableSorts { get; } = [];

    [ObservableProperty] private SortDescriptor? _selectedSort;

    [ObservableProperty] private bool _isSortDescending;

    /// <summary>
    ///     Сортировки, сгруппированные по категориям
    /// </summary>
    public ObservableCollection<IGrouping<string, SortDescriptor>> AvailableSortsByCategory { get; } = [];

    // Менеджер фильтров
    private readonly FilterManager _filterManager = new();
    private readonly EventTypeFilter _eventTypeFilter = new();
    private readonly UserNameFilter _userNameFilter = new();

    [ObservableProperty] public partial EventTypeFilterViewModel? EventTypeFilterViewModel { get; set; }

    // Хранит события по хешу файла для O(1) удаления группы событий
    // HashSet<EventBase> работает через record structural equality — 
    // используем List + индекс для сохранения порядка вставки
    private readonly Dictionary<string, List<EventBase>> _eventsByFileHash = [];

    // Токен для отмены текущей операции загрузки
    private CancellationTokenSource? _loadingCts;

    public ObservableCollection<FileCardViewModel> TraceFileInfos { get; } = [];
    public ObservableCollection<FilterCardModel> FilterCardModels { get; } = [];

    /// <summary>
    ///     Отфильтрованные события на основе всех активных фильтров.
    ///     Обновляется при изменении фильтров.
    /// </summary>
    public ObservableCollection<EventBase> FilteredEvents { get; } = [];

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

    [ObservableProperty] public partial bool IsTraceFilesSectionVisible { get; set; }
    [ObservableProperty] public partial bool IsSearchSectionVisible { get; set; }
    [ObservableProperty] public partial bool IsEventsSectionVisible { get; set; }
    [ObservableProperty] public partial bool IsStatisticsSectionVisible { get; set; }
    [ObservableProperty] public partial bool IsLogsSectionVisible { get; set; }


    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsFileLoading { get; set; }

    [ObservableProperty] public partial double LoadProgress { get; set; }

    /// <summary>Design-time конструктор — только для XAML превью.</summary>
    public MainWindowViewModel()
    {
        _parser = null!;
        _fileDialogService = null!;
        _sortingService = null!;
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

        StatisticInfoModels.UpdateStatistics([
            new StatisticInfoModel("Файлов:", TraceFileInfos.Count.ToString()),
            new StatisticInfoModel("Всего событий:", "0"),
            new StatisticInfoModel("Отфильтрованных событий:", "0"),
            new StatisticInfoModel("Время парсинга:", "0 сек")
        ]);
        StatusMessage = "Готово (Design Time).";
        IsClassicSearch = true;
        GoToFactorySettingsSection();
    }

    /// <summary>Runtime конструктор — используется DI контейнером.</summary>
    public MainWindowViewModel(IFileDialogService fileDialogService, ITraceLogParser parser,
        ISortingService sortingService, IOptions<AppSettings> appSettings,
        IOptions<UiSectionSettings> uiSettings)
    {
        // Инициализируем фильтры
        InitializeFilters();

        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        // Извлекаем значения из IOptions<T>
        _appSettings = appSettings.Value;
        _uiSettings = uiSettings.Value;
        _sortingService = sortingService;

        StatisticInfoModels = new StatisticsInfoSectionViewModel();

        // Регистрируем пользовательскую сортировку
        _sortingService.RegisterCustomSort(new SortDescriptor(
            "custom_user_activity",
            "По активности пользователя",
            (a, b, desc) => CustomUserActivityComparer(a, b, desc),
            "Аналитика",
            50));

        _sortingService.RegisterCustomSort(new SortDescriptor(
            "heavy_queries",
            "Тяжёлые запросы",
            (a, b, desc) => HeavyQueriesComparer(a, b, desc),
            "Аналитика",
            2));

        UpdateAvailableSorts();

        CurrentSearchType = SearchType.Classic;
        IsClassicSearch = true;
        GoToFactorySettingsSection();
        LoadSettings();
        StatusMessage = "Готов к работе!";

        Logger.Info("MainWindowViewModel инициализирован");
    }
    
    /// <summary>
    /// Загружает настройки из конфигурации
    /// </summary>
    private void LoadSettings()
    {
        // UI секции
        IsTraceFilesSectionVisible = _uiSettings.Files;
        IsSearchSectionVisible = _uiSettings.Search;
        IsEventsSectionVisible = _uiSettings.Events;
        IsStatisticsSectionVisible = _uiSettings.Statistics;
        IsLogsSectionVisible = _uiSettings.Logs;

        Logger.Info("Настройки UI загружены: Files={Files}, Search={Search}, Events={Events}",
            _uiSettings.Files, _uiSettings.Search, _uiSettings.Events);
        
    }

    private int HeavyQueriesComparer(EventBase a, EventBase b, bool descending)
    {
        var msA = GetExecuteMs(a);
        var msB = GetExecuteMs(b);

        // Сначала тяжёлые (по убыванию времени)
        var result = msB.CompareTo(msA);
        //return result != 0 ? result : a.Timestamp.CompareTo(b.Timestamp);
        
        if (result == 0)
            result = a.Timestamp.CompareTo(b.Timestamp);
        
        return descending ? -result : result;
    }
    
    /// <summary>
    ///     Пример пользовательской сортировки.
    /// </summary>
    private int CustomUserActivityComparer(EventBase a, EventBase b, bool descending)
    {
        var userA = GetUserFromEvent(a);
        var userB = GetUserFromEvent(b);

        // null всегда в конце
        if (userA == null && userB == null) return 0;
        if (userA == null) return 1;
        if (userB == null) return -1;

        var result = string.Compare(userA, userB, StringComparison.OrdinalIgnoreCase);
    
        // Если пользователи одинаковые, сортируем по времени
        if (result == 0)
            result = a.Timestamp.CompareTo(b.Timestamp);
    
        return descending ? -result : result; // ← инвертируем для descending
    }

    private int GetExecuteMs(EventBase evt)
    {
        return evt switch
        {
            StatementFinishEvent e => e.Performance.ExecuteMs,
            ProcedureFinishEvent e => e.Performance.ExecuteMs,
            TriggerFinishEvent e => e.Performance.ExecuteMs,
            _ => 0
        };
    }

    [RelayCommand]
    private void SelectSort(SortDescriptor? descriptor)
    {
        if (descriptor == null || descriptor == SelectedSort)
            return;

        // Снимаем выделение с предыдущей
        if (SelectedSort != null)
            SelectedSort.IsSelected = false;

        // Устанавливаем новую
        SelectedSort = descriptor;
        descriptor.IsSelected = true;

        Logger.Info("Выбрана сортировка: {DisplayName}", descriptor.DisplayName);
    }

    private void UpdateAvailableSorts()
    {
        var previousSelectedId = SelectedSort?.Id;

        AvailableSorts.Clear();
        AvailableSortsByCategory.Clear();

        var sorts = _sortingService.GetAvailableSorts(FilteredEvents);

        // Добавляем в обычную коллекцию
        foreach (var sort in sorts) AvailableSorts.Add(sort);

        // Группируем по категориям
        var grouped = sorts
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped) AvailableSortsByCategory.Add(group);

        // Восстанавливаем выбор
        SortDescriptor? toSelect = null;

        if (previousSelectedId != null)
            toSelect = sorts.FirstOrDefault(s => s.Id == previousSelectedId);

        toSelect ??= sorts.FirstOrDefault(s => s.IsDefault) ?? sorts.FirstOrDefault();

        if (toSelect != null)
        {
            toSelect.IsSelected = true;
            SelectedSort = toSelect;
        }
    }

    /// <summary>
    ///     Применяет текущую сортировку.
    /// </summary>
    partial void OnSelectedSortChanged(SortDescriptor? value)
    {
        if (value == null) return;
        ApplyCurrentSort();
    }

    partial void OnIsSortDescendingChanged(bool value)
    {
        ApplyCurrentSort();
    }

    private void ApplyCurrentSort()
    {
        if (SelectedSort == null) return;

        var sorted = _sortingService.ApplySort(
            FilteredEvents,
            SelectedSort.Id,
            IsSortDescending);

        // Обновляем коллекцию
        FilteredEvents.Clear();
        foreach (var evt in sorted) FilteredEvents.Add(evt);

        StatusMessage = $"Применена сортировка: {SelectedSort.DisplayName}";
    }

    

    private string? GetUserFromEvent(EventBase evt)
    {
        return evt switch
        {
            AttachDatabaseEvent e => e.Attachment.User,
            DetachDatabaseEvent e => e.Attachment.User,
            StatementEventBase e => e.Attachment.User,
            ProcedureEventBase e => e.Attachment.User,
            TriggerEventBase e => e.Attachment.User,
            _ => null
        };
    }

    private void ApplyAllFilters()
    {
        FilteredEvents.Clear();

        foreach (var evt in Events)
            if (_filterManager.IsEventVisible(evt))
                FilteredEvents.Add(evt);

        UpdateStatistics();
        UpdateAvailableSorts(); // ← Обновляем сортировки после фильтрации
        ApplyCurrentSort(); // ← Применяем текущую сортировку

        StatusMessage = $"Отфильтровано: {FilteredEvents.Count}/{Events.Count} событий";
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
    }

    [RelayCommand]
    private void SwitchVisibleTraceFilesSection()
    {
        IsTraceFilesSectionVisible = !IsTraceFilesSectionVisible;
    }

    [RelayCommand]
    private void SwitchVisibleSearchSection()
    {
        IsSearchSectionVisible = !IsSearchSectionVisible;
    }

    [RelayCommand]
    private void SwitchEventsSectionVisible()
    {
        IsEventsSectionVisible = !IsEventsSectionVisible;
    }

    [RelayCommand]
    private void SwitchStatisticsSectionVisible()
    {
        IsStatisticsSectionVisible = !IsStatisticsSectionVisible;
    }

    [RelayCommand]
    private void SwitchLogsSectionVisible()
    {
        IsLogsSectionVisible = !IsLogsSectionVisible;
    }

    [RelayCommand]
    private void GoToFactorySettingsSection()
    {
        IsTraceFilesSectionVisible = _uiSettings.Files;
        IsSearchSectionVisible = _uiSettings.Search;
        IsEventsSectionVisible = _uiSettings.Events;
        IsStatisticsSectionVisible = _uiSettings.Statistics;
        IsLogsSectionVisible = _uiSettings.Logs;

        Logger.Info("Восстановлены заводские настройки");
        StatusMessage = "Восстановлены заводские настройки";
    }


    [RelayCommand]
    private void ClearFilters()
    {
        _filterManager.ResetAll();

        // Сбрасываем UI
        if (EventTypeFilterViewModel != null)
            foreach (var checkBox in EventTypeFilterViewModel.EventTypeCheckBoxes)
                checkBox.IsSelected = false;

        ApplyAllFilters();
        StatusMessage = "Все фильтры очищены.";
    }

    /// <summary>
    ///     Инициализирует все доступные фильтры.
    ///     Место, где добавляются новые фильтры в будущем.
    /// </summary>
    private void InitializeFilters()
    {
        // Регистрируем фильтр типов событий
        _filterManager.RegisterFilter(_eventTypeFilter);
        _filterManager.RegisterFilter(_userNameFilter);

        // Создаём ViewModel для UI
        EventTypeFilterViewModel = new EventTypeFilterViewModel(
            _eventTypeFilter,
            ApplyAllFilters // Callback при изменении
        );

        Logger.Info("Фильтры инициализированы");
    }

    /// <summary>
    ///     Добавляет новое событие и применяет фильтры.
    ///     Вызывается после парсинга файла.
    /// </summary>
    private void AddEventWithFiltering(EventBase evt)
    {
        Events.Add(evt);

        if (_filterManager.IsEventVisible(evt))
            FilteredEvents.Add(evt);
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
            ApplyAllFilters();
            //UpdateAvailableSorts();
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
            ApplyAllFilters();
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
        // Блокируем повторный вызов
        IsFileLoading = true;
        OpenLocalFileCommand.NotifyCanExecuteChanged();

        IReadOnlyList<IStorageFile>? files = null;

        CancellationTokenSource? cts = null;
        try
        {
            // Создаём связанный CTS: отмена через кнопку ИЛИ через cancellationToken фреймворка
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loadingCts = cts;
            
            var token = _loadingCts.Token;
            
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
            StatusMessage = "Загрузка файлов отменена.";
            Logger.Info("Загрузка файлов отменена пользователем");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при загрузке файлов");
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            if (cts != null)
            {
                _loadingCts = null;
                cts.Dispose();
            }
            
            IsFileLoading = false;
            LoadProgress = 0;
            ApplyAllFilters();
            //UpdateAvailableSorts();

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
    ///     При изменении IsFileLoading уведомляем все зависимые команды.
    ///     Централизованно — не нужно дублировать NotifyCanExecuteChanged везде.
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
                AddEventWithFiltering(evt);
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

        // Удаляем из обеих коллекций (исходной и отфильтрованной)
        for (var i = Events.Count - 1; i >= 0; i--)
            if (eventsSet.Contains(Events[i]))
                Events.RemoveAt(i);

        for (var i = FilteredEvents.Count - 1; i >= 0; i--)
            if (eventsSet.Contains(FilteredEvents[i]))
                FilteredEvents.RemoveAt(i);


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
    ///     Повторно парсит один файл.
    ///     Используется и как callback из FileCardViewModel, и из команд меню.
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

            ApplyAllFilters();
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
    ///     Создаёт ViewModel для карточки файла с привязкой callbacks.
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
            new StatisticInfoModel(
                "Отфильтрованных событий:",
                FilteredEvents.Count.ToString("N0"))
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