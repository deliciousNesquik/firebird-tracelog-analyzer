using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Services.Filtering;
using NLog;

namespace FirebirdTraceViewer.ViewModels;

public partial class FiltersPanelViewModel : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Action _onFiltersApplied; // ← Переименовали для ясности

    /// <summary>Все доступные фильтры</summary>
    public ObservableCollection<FilterDescriptor> AvailableFilters { get; } = [];

    /// <summary>Фильтры, сгруппированные по категориям</summary>
    public ObservableCollection<IGrouping<string, FilterDescriptor>> FiltersByCategory { get; } = [];

    /// <summary>Количество активных фильтров</summary>
    [ObservableProperty]
    private int _activeFiltersCount;

    /// <summary>Есть ли изменения (нужно применить фильтры)</summary>
    [ObservableProperty]
    private bool _hasUnappliedChanges;

    public FiltersPanelViewModel(Action onFiltersApplied)
    {
        _onFiltersApplied = onFiltersApplied ?? throw new ArgumentNullException(nameof(onFiltersApplied));
    }

    /// <summary>
    /// ✅ ЯВНОЕ применение фильтров (по кнопке)
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        _onFiltersApplied();
        HasUnappliedChanges = false;
        Logger.Info("Фильтры применены вручную");
    }

    /// <summary>
    /// Загружает фильтры и подписывается на изменения.
    /// </summary>
    public void LoadFilters(IEnumerable<FilterDescriptor> filters)
    {
        // Отписываемся от старых
        foreach (var filter in AvailableFilters)
        {
            filter.PropertyChanged -= OnFilterPropertyChanged;

            foreach (var value in filter.AvailableValues)
                value.PropertyChanged -= OnFilterValueChanged;
        }

        AvailableFilters.Clear();
        FiltersByCategory.Clear();

        foreach (var filter in filters)
        {
            AvailableFilters.Add(filter);

            // Подписываемся на изменения
            filter.PropertyChanged += OnFilterPropertyChanged;

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
        HasUnappliedChanges = false;
    }

    /// <summary>
    /// Обновляет счётчики фильтров на основе отфильтрованных событий
    /// </summary>
    public void UpdateFilterCounts(IEnumerable<EventBase> filteredEvents)
    {
        var eventsList = filteredEvents.ToList();

        foreach (var filter in AvailableFilters)
        {
            // Пропускаем активные фильтры (не пересчитываем их счётчики)
            if (filter.IsActive)
                continue;

            // Обновляем счётчики для Enum/String фильтров
            if (filter.FilterType is FilterType.EnumMultiSelect or FilterType.StringMultiSelect)
            {
                UpdateMultiSelectCounts(filter, eventsList);
            }
        }

        Logger.Debug("Счётчики фильтров обновлены для {Count} событий", eventsList.Count);
    }

    private void UpdateMultiSelectCounts(FilterDescriptor filter, List<EventBase> events)
    {
        var valueCounts = new Dictionary<object, int>();

        foreach (var evt in events)
        {
            var value = GetPropertyValue(evt, filter.PropertyPath);
            if (value != null)
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        // Обновляем счётчики в UI
        foreach (var item in filter.AvailableValues)
        {
            item.Count = valueCounts.TryGetValue(item.Value, out var count) ? count : 0;
        }
    }

    private object? GetPropertyValue(object obj, string propertyPath)
    {
        var parts = propertyPath.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current == null) return null;

            var prop = current.GetType().GetProperty(part);
            if (prop == null) return null;

            current = prop.GetValue(current);
        }

        return current;
    }

    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilterDescriptor.IsActive))
        {
            UpdateActiveFiltersCount();
            MarkAsChanged(); // ← Помечаем, что нужно применить
        }
        else if (e.PropertyName is nameof(FilterDescriptor.CurrentMinValue) or nameof(FilterDescriptor.CurrentMaxValue))
        {
            MarkAsChanged();
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

            MarkAsChanged();
        }
    }

    private void MarkAsChanged()
    {
        HasUnappliedChanges = true;
    }

    [RelayCommand]
    private void ResetAllFilters()
    {
        foreach (var filter in AvailableFilters)
        {
            filter.Reset();
        }

        UpdateActiveFiltersCount();
        HasUnappliedChanges = true; // Нужно применить сброс

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