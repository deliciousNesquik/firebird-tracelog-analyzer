using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceViewer.Services;
using NLog;

namespace FirebirdTraceViewer.ViewModels;

public partial class FiltersPanelViewModel : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Action _onFiltersChanged;

    /// <summary>Все доступные фильтры</summary>
    public ObservableCollection<FilterDescriptor> AvailableFilters { get; } = [];

    /// <summary>Фильтры, сгруппированные по категориям</summary>
    public ObservableCollection<IGrouping<string, FilterDescriptor>> FiltersByCategory { get; } = [];

    /// <summary>Количество активных фильтров</summary>
    [ObservableProperty]
    private int _activeFiltersCount;

    public FiltersPanelViewModel(Action onFiltersChanged)
    {
        _onFiltersChanged = onFiltersChanged ?? throw new ArgumentNullException(nameof(onFiltersChanged));
    }
    
    [RelayCommand]
    private void ApplyFilters()
    {
        _onFiltersChanged();
    }

    /// <summary>
    /// Загружает фильтры и подписывается на изменения.
    /// </summary>
    public void LoadFilters(IEnumerable<FilterDescriptor> filters)
    {
        // Отписываемся от старых
        foreach (var filter in AvailableFilters)
        {
            filter.PropertyChanged -= OnFilterChanged;
            filter.RangeValueChanged -= OnRangeValueChanged; // ← Новое
            
            foreach (var value in filter.AvailableValues)
                value.PropertyChanged -= OnFilterValueChanged;
        }

        AvailableFilters.Clear();
        FiltersByCategory.Clear();

        foreach (var filter in filters)
        {
            AvailableFilters.Add(filter);
            
            // Подписываемся на изменения
            filter.PropertyChanged += OnFilterChanged;
            filter.RangeValueChanged += OnRangeValueChanged; // ← Новое
            
            foreach (var value in filter.AvailableValues)
                value.PropertyChanged += OnFilterValueChanged;
        }

        // Группируем по категориям
        var grouped = AvailableFilters
            .GroupBy(f => f.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
            FiltersByCategory.Add(group);

        UpdateActiveFiltersCount();
    }

    private void OnRangeValueChanged(object? sender, EventArgs e)
    {
        // При изменении диапазона автоматически активируем фильтр и применяем
        if (sender is FilterDescriptor filter)
        {
            if (!filter.IsActive)
                filter.IsActive = true;

            _onFiltersChanged(); // ← Применяем фильтры
        }
    }

    private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilterDescriptor.IsActive))
        {
            UpdateActiveFiltersCount();
            _onFiltersChanged();
        }
    }

    private void OnFilterValueChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilterValueItem.IsSelected))
        {
            if (sender is FilterValueItem item)
            {
                // Находим родительский фильтр
                var parentFilter = AvailableFilters.FirstOrDefault(f => f.AvailableValues.Contains(item));
                if (parentFilter != null)
                {
                    // Автоматически активируем фильтр при выборе значения
                    var hasSelected = parentFilter.AvailableValues.Any(v => v.IsSelected);
                    parentFilter.IsActive = hasSelected;
                }
            }

            _onFiltersChanged();
        }
    }

    [RelayCommand]
    private void ResetAllFilters()
    {
        foreach (var filter in AvailableFilters)
        {
            filter.IsActive = false;
            
            foreach (var value in filter.AvailableValues)
                value.IsSelected = false;

            // Сброс диапазонов
            filter.CurrentMinValue = filter.MinValue;
            filter.CurrentMaxValue = filter.MaxValue;
            filter.SearchText = null;
        }

        UpdateActiveFiltersCount();
        _onFiltersChanged();
        
        Logger.Info("Все фильтры сброшены");
    }

    [RelayCommand]
    private void ToggleFilter(FilterDescriptor filter)
    {
        filter.IsActive = !filter.IsActive;
    }

    private void UpdateActiveFiltersCount()
    {
        ActiveFiltersCount = AvailableFilters.Count(f => f.IsActive);
    }
}