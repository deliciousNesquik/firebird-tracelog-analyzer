using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceParser.Models.Events;
using FirebirdTraceParser.Parsing.Engine;
using FirebirdTraceParser.Parsing.Utils;
using FirebirdTraceAnalyzer.Core;
using FirebirdTraceAnalyzer.Enums;
using FirebirdTraceAnalyzer.Interfaces;
using FirebirdTraceAnalyzer.Mocks;
using FirebirdTraceAnalyzer.Models;
using FirebirdTraceAnalyzer.Services.Filtering;
using FirebirdTraceAnalyzer.Services.Searching;
using FirebirdTraceAnalyzer.Services.Sorting;
using Microsoft.Extensions.Options;
using NLog;

namespace FirebirdTraceAnalyzer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region Dependencies (Injected Services)

    private readonly AppSettings _appSettings;
    private readonly UiSectionSettings _uiSettings;
    private readonly IFileDialogService _fileDialogService;
    private readonly ITraceLogParser _parser;
    private readonly ISortingService _sortingService;
    private readonly IFilteringService _filteringService;
    private readonly ISearchService _searchService;

    #endregion

    #region Collections

    /// <summary>Все события из всех файлов (source of truth)</summary>
    private List<EventBase> AllEvents { get; } = [];

    /// <summary>События после применения фильтров и сортировки</summary>
    public RangeObservableCollection<EventBase> VisibleEvents { get; } = [];

    /// <summary>Карточки загруженных файлов</summary>
    public ObservableCollection<FileCardViewModel> FileCards { get; } = [];

    /// <summary>Выделенные карточки загруженных файлов</summary>
    public ObservableCollection<FileCardViewModel> SelectedFileCards { get; } = [];

    /// <summary>Сортировки, сгруппированные по категориям</summary>
    public ObservableCollection<IGrouping<string, SortDescriptor>> AvailableSortsByCategory { get; } = [];

    #endregion

    #region State Management

    // События по хешу файла (для быстрого удаления)
    private readonly Dictionary<string, List<EventBase>> _eventsByFileHash = [];

    // Токен отмены загрузки
    private CancellationTokenSource? _loadingCts;

    // ✅ Флаг пакетного обновления (для предотвращения множественных пересчётов)
    private bool _isBatchUpdate;

    #endregion

    #region Observable Properties - UI State

    [ObservableProperty] private bool _isTraceFilesSectionVisible;
    [ObservableProperty] private bool _isSearchSectionVisible;
    [ObservableProperty] private bool _isEventsSectionVisible;
    [ObservableProperty] private bool _isStatisticsSectionVisible;
    [ObservableProperty] private bool _isLogsSectionVisible;

    [ObservableProperty] private SearchType _currentSearchType;
    [ObservableProperty] private bool _isClassicSearch;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isFileLoading;
    [ObservableProperty] private double _loadProgress;

    #endregion

    #region Observable Properties - Sorting & Filtering & Search

    [ObservableProperty] private SortDescriptor? _selectedSort;
    [ObservableProperty] private bool _isSortDescending;
    
    [ObservableProperty] 
    private string _searchText = string.Empty;

    [ObservableProperty] 
    private bool _isSearchActive;

    /// <summary>ViewModel панели фильтров</summary>
    public FiltersPanelViewModel FiltersPanelViewModel { get; }

    /// <summary>ViewModel секции статистики</summary>
    public StatisticsInfoSectionViewModel StatisticInfoModels { get; }

    #endregion

    #region Constructors

    /// <summary>Design-time конструктор (для XAML превью)</summary>
    public MainWindowViewModel()
    {
        // Mock-данные для дизайнера
        _appSettings = new AppSettingsMock();
        _uiSettings = new UiSectionSettingsMock();
        _parser = null!;
        _fileDialogService = null!;
        _sortingService = null!;
        _filteringService = null!;
        _searchService = null!;

        // Инициализация ViewModels
        StatisticInfoModels = new StatisticsInfoSectionViewModel();
        FiltersPanelViewModel = new FiltersPanelViewModel(ApplyAllFilters);

        StatisticInfoModels.UpdateStatistics([
            new StatisticInfoModel("Files:", FileCards.Count.ToString()),
            new StatisticInfoModel("All Events:", AllEvents.Count.ToString()),
            new StatisticInfoModel("Visible Events:", VisibleEvents.Count.ToString()),
            new StatisticInfoModel("Filtered Events:", AllEvents.Count.ToString())
        ]);

        LoadSettings();
        StatusMessage = "Ready to go (Design Time).";
    }

    /// <summary>Runtime конструктор (DI)</summary>
    public MainWindowViewModel(
        IFileDialogService fileDialogService,
        ITraceLogParser parser,
        ISortingService sortingService,
        IFilteringService filteringService,
        ISearchService searchService,
        IOptions<AppSettings> appSettings,
        IOptions<UiSectionSettings> uiSettings)
    {
        
        
        Logger.Info("Object(s) pool have been reset(s).");
        StringPool.Reset();
        TraceSessionInfoPool.Reset();
        AttachmentInfoPool.Reset();
        
        Logger.Info("Event(s) list(s) are clear");
        VisibleEvents.Clear();
        AllEvents.Clear();
        Logger.Debug($"VisibleEvents count: {VisibleEvents.Count}");
        Logger.Debug($"AllEvents count: {AllEvents.Count}");
        
        
        // Dependency Injection
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _sortingService = sortingService ?? throw new ArgumentNullException(nameof(sortingService));
        _filteringService = filteringService ?? throw new ArgumentNullException(nameof(filteringService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _uiSettings = uiSettings.Value ?? throw new ArgumentNullException(nameof(uiSettings));

        // Инициализация ViewModels
        StatisticInfoModels = new StatisticsInfoSectionViewModel();
        FiltersPanelViewModel = new FiltersPanelViewModel(ApplyAllFilters);

        // Регистрация пользовательских сортировок
        RegisterCustomSorts();

        // Загрузка настроек
        LoadSettings();

        StatusMessage = "Ready to go!";
        Logger.Info("MainWindowViewModel initialized.");
    }

    #endregion

    #region Initialization

    /// <summary>Загружает настройки из конфигурации</summary>
    private void LoadSettings()
    {
        // UI Visibility
        IsTraceFilesSectionVisible = _uiSettings.Files;
        IsSearchSectionVisible = _uiSettings.Search;
        IsEventsSectionVisible = _uiSettings.Events;
        IsStatisticsSectionVisible = _uiSettings.Statistics;
        IsLogsSectionVisible = _uiSettings.Logs;

        // Search Type
        IsClassicSearch = _appSettings.IsClassicSearch;
        CurrentSearchType = IsClassicSearch ? SearchType.Classic : SearchType.Regex;

        Logger.Info("Application settings loaded.");
        StatusMessage = "Application settings loaded.";
    }

    /// <summary>Регистрирует пользовательские сортировки</summary>
    private void RegisterCustomSorts()
    {
        _sortingService.RegisterCustomSort(new SortDescriptor(
            "custom_user_activity",
            "User Activity",
            CustomUserActivityComparer,
            false,
            "Analytics",
            50));

        _sortingService.RegisterCustomSort(new SortDescriptor(
            "heavy_queries",
            "Heavy Queries",
            HeavyQueriesComparer,
            false,
            "Analytics",
            2));

        Logger.Info("Custom sorts registered.");
    }

    #endregion

    #region Sorting

    /// <summary>Обновляет список доступных сортировок</summary>
    private void UpdateAvailableSorts()
    {
        var previousSelectedId = SelectedSort?.Id;

        AvailableSortsByCategory.Clear();

        // Передаём ВИДИМЫЕ события (после фильтрации)
        var sorts = _sortingService.GetAvailableSorts(VisibleEvents);

        var grouped = sorts
            .GroupBy(s => s.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
            AvailableSortsByCategory.Add(group);

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

        Logger.Info("Available sorts updated: {Count}", sorts.Count);
    }

    /// <summary>Применяет текущую сортировку</summary>
    private void ApplyCurrentSort()
    {
        if (SelectedSort == null)
        {
            Logger.Warn("No sort selected, skipping sorting.");
            return;
        }

        var sorted = _sortingService.ApplySort(
            VisibleEvents,
            SelectedSort.Id,
            IsSortDescending);

        VisibleEvents.ReplaceRange(sorted);

        StatusMessage =
            $"Sorted by: {SelectedSort.DisplayName} ({(IsSortDescending ? "desc" : "asc")})";

        Logger.Info(
            "Applied sort: {SortName}, descending={Descending}",
            SelectedSort.DisplayName,
            IsSortDescending);
    }

    [RelayCommand]
    private void SelectSort(SortDescriptor? descriptor)
    {
        if (descriptor == null || descriptor == SelectedSort)
            return;

        if (SelectedSort != null)
            SelectedSort.IsSelected = false;

        SelectedSort = descriptor;
        descriptor.IsSelected = true;

        Logger.Info("Sort selected: {DisplayName}", descriptor.DisplayName);
    }

    partial void OnSelectedSortChanged(SortDescriptor? value)
    {
        if (value != null && !_isBatchUpdate)
            ApplyCurrentSort();
    }

    partial void OnIsSortDescendingChanged(bool value)
    {
        if (!_isBatchUpdate)
            ApplyCurrentSort();
    }

    #region Custom Sort Comparers

    private int CustomUserActivityComparer(EventBase a, EventBase b, bool descending)
    {
        var userA = GetUserFromEvent(a);
        var userB = GetUserFromEvent(b);

        if (userA == null && userB == null) return 0;
        if (userA == null) return 1;
        if (userB == null) return -1;

        var result = string.Compare(userA, userB, StringComparison.OrdinalIgnoreCase);

        if (result == 0)
            result = a.Timestamp.CompareTo(b.Timestamp);

        return descending ? -result : result;
    }

    private int HeavyQueriesComparer(EventBase a, EventBase b, bool descending)
    {
        var msA = GetExecuteMs(a);
        var msB = GetExecuteMs(b);

        var result = msB.CompareTo(msA); // По умолчанию тяжёлые первыми

        if (result == 0)
            result = a.Timestamp.CompareTo(b.Timestamp);

        return descending ? -result : result;
    }

    private static int GetExecuteMs(EventBase evt)
    {
        return evt switch
        {
            StatementFinishEvent e => e.Performance.ExecuteMs,
            ProcedureFinishEvent e => e.Performance.ExecuteMs,
            TriggerFinishEvent e => e.Performance.ExecuteMs,
            _ => 0
        };
    }

    private static string? GetUserFromEvent(EventBase evt)
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

    #endregion

    #endregion

    #region Filtering

    /// <summary>
    /// Обновляет доступные фильтры на основе ТЕКУЩИХ (отфильтрованных) событий
    /// </summary>
    private void UpdateAvailableFilters()
    {
        // Передаём VisibleEvents вместо AllEvents!
        // Это позволяет показывать фильтры только для видимого типа события
        var filters = _filteringService.GetAvailableFilters(VisibleEvents);

        FiltersPanelViewModel.LoadFilters(filters);

        StatusMessage = $"Available filters: {filters.Count}";
        Logger.Info("Available filters updated: {Count}", filters.Count);
    }

    /// <summary>
    /// Применяет все активные фильтры и обновляет UI
    /// </summary>
    private void ApplyAllFilters()
    {
        try
        {
            _isBatchUpdate = true;
            
            Logger.Info("Starting to use filters and search...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            IEnumerable<EventBase> query = AllEvents;

            // СНАЧАЛА поиск (если активен)
            if (IsSearchActive && !string.IsNullOrWhiteSpace(SearchText))
            {
                var searchMode = IsClassicSearch ? SearchType.Classic : SearchType.Regex;
                query = _searchService.Search(query, SearchText, searchMode);
                
                var searchResults = query.ToList();
                Logger.Info("Search completed in {Elapsed}ms, found: {Count}",
                    sw.ElapsedMilliseconds, searchResults.Count);
                query = searchResults;
                sw.Restart();
            }

            // Применяем фильтры
            query = _filteringService.ApplyFilters(
                query,
                FiltersPanelViewModel.AvailableFilters);

            var filteredList = query.ToList();

            Logger.Info("Filtering completed in {Elapsed}ms, resulting in: {Count} events",
                sw.ElapsedMilliseconds, filteredList.Count);

            // Применяем сортировку (если есть)
            if (SelectedSort != null)
            {
                sw.Restart();
                filteredList = _sortingService.ApplySort(
                    filteredList,
                    SelectedSort.Id,
                    IsSortDescending).ToList();

                Logger.Info("Sorting completed in {Elapsed}ms", sw.ElapsedMilliseconds);
            }

            // Обновляем UI (одним батчем)
            sw.Restart();
            VisibleEvents.ReplaceRange(filteredList);
            Logger.Info("UI updated in {Elapsed}ms", sw.ElapsedMilliseconds);

            // СНАЧАЛА обновляем сортировки (для видимых типов)
            sw.Restart();
            UpdateAvailableSorts();
            Logger.Info("Sortings updated in {Elapsed}ms", sw.ElapsedMilliseconds);

            // ПОТОМ обновляем фильтры (для видимых типов)
            sw.Restart();
            UpdateAvailableFilters();
            Logger.Info("Filters updated in {Elapsed}ms", sw.ElapsedMilliseconds);

            // Обновляем счётчики фильтров
            sw.Restart();
            FiltersPanelViewModel.UpdateFilterCounts(filteredList);
            Logger.Info("Filter counters updated in {Elapsed}ms", sw.ElapsedMilliseconds);

            // Обновляем статистику
            UpdateStatistics();

            var statusParts = new List<string>();
            
            if (IsSearchActive)
                statusParts.Add($"Search: '{SearchText}'");
            
            statusParts.Add($"{filteredList.Count:N0} of {AllEvents.Count:N0}");

            StatusMessage = string.Join(" • ", statusParts);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error applying filters");
            StatusMessage = $"Filtering error: {ex.Message}";
        }
        finally
        {
            _isBatchUpdate = false;
        }
    }

    #endregion
    
    #region Search

    /// <summary>
    /// Выполняет поиск (вызывается кнопкой)
    /// </summary>
    [RelayCommand]
    private void ExecuteSearch()
    {
        IsSearchActive = !string.IsNullOrWhiteSpace(SearchText);
        
        if (!IsSearchActive)
        {
            StatusMessage = "Search query is empty";
            Logger.Warn("Attempted search with empty query");
        }
        
        // Применяем фильтры + поиск
        ApplyAllFilters();
    }

    /// <summary>
    /// Сбрасывает поиск
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        IsSearchActive = false;
        ApplyAllFilters();
        
        StatusMessage = "Search reset";
        Logger.Info("Search reset");
    }

    #endregion
    
    #region File Operations

    private bool CanOpenFile()
    {
        return !IsFileLoading;
    }

    /// <summary>Открывает диалог выбора файлов</summary>
    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenLocalFileAsync(CancellationToken cancellationToken)
    {
        IsFileLoading = true;
        OpenLocalFileCommand.NotifyCanExecuteChanged();

        CancellationTokenSource? cts = null;

        try
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loadingCts = cts;

            var files = await _fileDialogService.PickTraceFilesAsync();

            if (files.Count == 0)
            {
                StatusMessage = "No files selected.";
                return;
            }

            await ProcessSelectedFilesAsync(files, cts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "File loading cancelled.";
            Logger.Info("File loading cancelled by user.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error loading files");
            StatusMessage = $"Loading error: {ex.Message}";
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
            OpenLocalFileCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanCancelLoading()
    {
        return IsFileLoading && _loadingCts != null;
    }

    /// <summary>Отменяет текущую загрузку</summary>
    [RelayCommand(CanExecute = nameof(CanCancelLoading))]
    private void CancelLoading()
    {
        _loadingCts?.Cancel();
        Logger.Info("Loading cancellation requested.");
    }

    /// <summary>Обрабатывает выбранные файлы</summary>
    private async Task ProcessSelectedFilesAsync(
        IReadOnlyList<IStorageFile> files,
        CancellationToken cancellationToken)
    {
        var addedCount = 0;
        var duplicateCount = 0;
        LoadProgress = 0;

        try
        {
            _isBatchUpdate = true;

            for (var i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var file = files[i];
                var path = file.Path.LocalPath;

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    Logger.Warn("File not found: {Path}", path);
                    continue;
                }

                StatusMessage = $"Processing {i + 1}/{files.Count}: {Path.GetFileName(path)}";
                LoadProgress = (double)(i + 1) / files.Count * 100;

                var fileHash = await CalculateFileHashAsync(path, cancellationToken);

                if (IsDuplicate(fileHash))
                {
                    duplicateCount++;
                    Logger.Warn("Duplicate file skipped: {FilePath}", path);
                    continue;
                }

                var fileInfo = new FileInfo(path);
                var traceModel = await ParseFileAsync(fileInfo, fileHash, cancellationToken);

                await Dispatcher.UIThread.InvokeAsync(() =>
                    FileCards.Add(CreateFileCardViewModel(traceModel)));

                addedCount++;
            }
        }
        finally
        {
            _isBatchUpdate = false;
        }

        // После загрузки всех файлов — ОДНО обновление
        if (addedCount > 0)
        {
            ApplyAllFilters(); // ← Применяет фильтры + обновляет сортировки + статистику
        }

        StatusMessage = BuildFileAddingStatusMessage(addedCount, duplicateCount);
    }

    /// <summary>Парсит один файл</summary>
    private async Task<TraceFileInfoModel> ParseFileAsync(
        FileInfo fileInfo,
        string fileHash,
        CancellationToken cancellationToken)
    {
        StatusMessage = $"Parsing: {fileInfo.Name}";

        Logger.Info("Streaming parse started: {FileName}", fileInfo.Name);

        var events = new List<EventBase>(8192);

        var startTrace = DateTime.MinValue;
        var endTrace = DateTime.MinValue;

        var batch = new List<EventBase>(1000);

        await using var stream = new FileStream(
            fileInfo.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            true);

        await foreach (var evt in _parser.ParseStreamAsync(
                           stream,
                           cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (startTrace == DateTime.MinValue)
                startTrace = evt.Timestamp;

            endTrace = evt.Timestamp;

            events.Add(evt);
            
            //TODO: пересмотреть данный участок
            batch.Add(evt);

            // НЕ обновляем VisibleEvents во время парсинга (будет обновлено после)
            if (batch.Count >= 1000) batch.Clear();
        }

        _eventsByFileHash[fileHash] = events;
        AllEvents.AddRange(events);

        Logger.Info(
            "Streaming parse completed: {FileName}, events: {Count}",
            fileInfo.Name,
            events.Count);

        return new TraceFileInfoModel(
            fileInfo.Name,
            fileInfo.FullName,
            fileInfo.Length,
            startTrace,
            endTrace,
            events.Count,
            fileHash);
    }

    /// <summary>Удаляет файл и его события</summary>
    private Task RemoveTraceFileAsync(FileCardViewModel card)
    {
        try
        {
            _isBatchUpdate = true;

            RemoveFileEvents(card.FileInfo.FileHash);
            FileCards.Remove(card);

            StatusMessage = $"File removed: {card.FileInfo.FileName}";
            Logger.Info("File removed: {FileName}", card.FileInfo.FileName);
        }
        finally
        {
            _isBatchUpdate = false;
        }

        ApplyAllFilters();

        return Task.CompletedTask;
    }

    /// <summary>Удаляет события файла из коллекций</summary>
    private void RemoveFileEvents(string fileHash)
    {
        if (!_eventsByFileHash.TryGetValue(fileHash, out var eventsToRemove))
            return;

        var eventsSet = eventsToRemove.ToHashSet();

        for (var i = AllEvents.Count - 1; i >= 0; i--)
            if (eventsSet.Contains(AllEvents[i]))
                AllEvents.RemoveAt(i);

        for (var i = VisibleEvents.Count - 1; i >= 0; i--)
            if (eventsSet.Contains(VisibleEvents[i]))
                VisibleEvents.RemoveAt(i);

        _eventsByFileHash.Remove(fileHash);

        Logger.Info("Removed {Count} events for file hash {Hash}", eventsToRemove.Count, fileHash);
    }

    private bool IsDuplicate(string fileHash)
    {
        return FileCards.Any(f =>
            string.Equals(f.FileInfo.FileHash, fileHash, StringComparison.OrdinalIgnoreCase));
    }

    private FileCardViewModel CreateFileCardViewModel(TraceFileInfoModel fileInfo)
    {
        return new FileCardViewModel(fileInfo, RemoveTraceFileAsync,
            card => ReparseTraceFileAsync(card, CancellationToken.None));
    }

    #endregion

    #region Reparse Operations

    [RelayCommand(CanExecute = nameof(CanReparseFiles))]
    private async Task ReparseAllFilesAsync(CancellationToken cancellationToken)
    {
        if (FileCards.Count == 0)
        {
            StatusMessage = "No files to reprocess.";
            return;
        }

        IsFileLoading = true;
        NotifyCommandsCanExecuteChanged();

        try
        {
            _isBatchUpdate = true;

            var allCards = FileCards.ToList();
            StatusMessage = $"Reprocessing all files: 0/{allCards.Count}";

            for (var i = 0; i < allCards.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var card = allCards[i];
                StatusMessage = $"Reprocessing {i + 1}/{allCards.Count}: {card.FileInfo.FileName}";

                await ReparseTraceFileAsync(card, cancellationToken);
            }

            StatusMessage = $"All files reprocessed: {allCards.Count} file(s).";
            Logger.Info("Reprocessing completed: {Count} files", allCards.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Reprocessing cancelled.";
            Logger.Info("Reprocessing cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during reprocessing");
            StatusMessage = $"Reprocessing error: {ex.Message}";
        }
        finally
        {
            _isBatchUpdate = false;
            IsFileLoading = false;

            UpdateAvailableFilters();
            ApplyAllFilters();

            NotifyCommandsCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanReparseSelectedFiles))]
    private async Task ReparseSelectedFilesAsync(CancellationToken cancellationToken)
    {
        if (SelectedFileCards.Count == 0)
        {
            StatusMessage = "No files selected for reprocessing.";
            return;
        }

        IsFileLoading = true;
        NotifyCommandsCanExecuteChanged();

        try
        {
            _isBatchUpdate = true;

            var selectedCards = SelectedFileCards.ToList();
            StatusMessage = $"Reprocessing selected files: 0/{selectedCards.Count}";

            for (var i = 0; i < selectedCards.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var card = selectedCards[i];
                StatusMessage = $"Reprocessing {i + 1}/{selectedCards.Count}: {card.FileInfo.FileName}";

                await ReparseTraceFileAsync(card, cancellationToken);
            }

            StatusMessage = $"Selected files reprocessed: {selectedCards.Count} file(s).";
            Logger.Info("Selected files reprocessed: {Count}", selectedCards.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Reprocessing cancelled.";
            Logger.Info("Selected files reprocessing cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error reprocessing selected files");
            StatusMessage = $"Reprocessing error: {ex.Message}";
        }
        finally
        {
            _isBatchUpdate = false;
            IsFileLoading = false;

            UpdateAvailableFilters();
            ApplyAllFilters();

            NotifyCommandsCanExecuteChanged();
        }
    }

    private async Task ReparseTraceFileAsync(FileCardViewModel card,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileInfo = new FileInfo(card.FileInfo.FilePath);

            if (!fileInfo.Exists)
            {
                StatusMessage = $"File not found: {card.FileInfo.FileName}";
                Logger.Warn("File not found for reparse: {Path}", card.FileInfo.FilePath);
                return;
            }

            RemoveFileEvents(card.FileInfo.FileHash);

            var updatedModel = await ParseFileAsync(fileInfo, card.FileInfo.FileHash, cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() => card.FileInfo = updatedModel);

            Logger.Info("File reparsed: {FileName}", card.FileInfo.FileName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error reparsing file: {FileName}", card.FileInfo.FileName);
            StatusMessage = $"Reparse error: {card.FileInfo.FileName}: {ex.Message}";
        }
    }

    private bool CanReparseFiles()
    {
        return !IsFileLoading && FileCards.Count > 0;
    }

    private bool CanReparseSelectedFiles()
    {
        return !IsFileLoading && SelectedFileCards.Count > 0;
    }

    #endregion

    #region Close File Operations

    /// <summary>Закрывает все файлы</summary>
    [RelayCommand(CanExecute = nameof(CanCloseAllFiles))]
    private void CloseAllFiles()
    {
        if (FileCards.Count == 0)
        {
            StatusMessage = "No files to close.";
            return;
        }

        try
        {
            _isBatchUpdate = true;

            var count = FileCards.Count;
            
            // Очищаем все коллекции
            FileCards.Clear();
            AllEvents.Clear();
            VisibleEvents.Clear();
            _eventsByFileHash.Clear();

            StatusMessage = $"Closed all files: {count} file(s).";
            Logger.Info("All files closed: {Count}", count);
        }
        finally
        {
            _isBatchUpdate = false;
        }

        // Обновляем UI после очистки
        UpdateAvailableFilters();
        UpdateAvailableSorts();
        UpdateStatistics();
    }

    /// <summary>Закрывает выбранные файлы</summary>
    [RelayCommand(CanExecute = nameof(CanCloseSelectedFiles))]
    private void CloseSelectedFiles()
    {
        if (SelectedFileCards.Count == 0)
        {
            StatusMessage = "No files selected to close.";
            return;
        }

        try
        {
            _isBatchUpdate = true;

            var selectedCards = SelectedFileCards.ToList();
            
            foreach (var card in selectedCards)
            {
                RemoveFileEvents(card.FileInfo.FileHash);
                FileCards.Remove(card);
            }

            StatusMessage = $"Closed selected files: {selectedCards.Count} file(s).";
            Logger.Info("Selected files closed: {Count}", selectedCards.Count);
        }
        finally
        {
            _isBatchUpdate = false;
        }

        // Обновляем UI после удаления
        ApplyAllFilters();
    }

    private bool CanCloseAllFiles()
    {
        return !IsFileLoading && FileCards.Count > 0;
    }

    private bool CanCloseSelectedFiles()
    {
        return !IsFileLoading && SelectedFileCards.Count > 0;
    }

    #endregion

    #region UI Commands

    [RelayCommand]
    private void SwitchSearchType()
    {
        CurrentSearchType = CurrentSearchType == SearchType.Classic
            ? SearchType.Regex
            : SearchType.Classic;
    }

    partial void OnCurrentSearchTypeChanged(SearchType value)
    {
        IsClassicSearch = value == SearchType.Classic;
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

        Logger.Info("Factory settings restored.");
        StatusMessage = "Factory settings restored.";
    }

    #endregion

    #region Utilities

    partial void OnIsFileLoadingChanged(bool value)
    {
        NotifyCommandsCanExecuteChanged();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        OpenLocalFileCommand.NotifyCanExecuteChanged();
        CancelLoadingCommand.NotifyCanExecuteChanged();
        ReparseAllFilesCommand.NotifyCanExecuteChanged();
        ReparseSelectedFilesCommand.NotifyCanExecuteChanged();
        CloseAllFilesCommand.NotifyCanExecuteChanged();
        CloseSelectedFilesCommand.NotifyCanExecuteChanged();
    }

    private void UpdateStatistics()
    {
        var totalEvents = FileCards.Sum(f => f.FileInfo.EventCount);

        StatisticInfoModels.UpdateStatistics([
            new StatisticInfoModel("Files:", FileCards.Count.ToString()),
            new StatisticInfoModel("All Events:", totalEvents.ToString("N0")),
            new StatisticInfoModel("Visible Events:", VisibleEvents.Count.ToString("N0"))
        ]);
    }

    private static async Task<string> CalculateFileHashAsync(string filePath,
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

    private static string BuildFileAddingStatusMessage(int addedCount, int duplicateCount)
    {
        return (addedCount, duplicateCount) switch
        {
            (> 0, > 0) => $"Loaded: {addedCount} file(s). Skipped duplicates: {duplicateCount}.",
            (> 0, 0) => $"Loaded: {addedCount} file(s).",
            (0, > 0) => "No files loaded: all files are duplicates.",
            _ => "No files selected."
        };
    }

    #endregion
}