using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.ViewModels;

namespace FirebirdTraceViewer.Services.Filtering;

/// <summary>
/// Описывает один фильтр со всеми его настройками.
/// </summary>
public partial class FilterDescriptor : ViewModelBase
{

    /// <summary>Уникальный ID фильтра</summary>
    public string Id { get; }

    /// <summary>Отображаемое имя</summary>
    public string DisplayName { get; }

    /// <summary>Категория</summary>
    public string Category { get; }

    /// <summary>Приоритет отображения</summary>
    public int Priority { get; }

    /// <summary>Тип фильтра (для UI)</summary>
    public FilterType FilterType { get; }

    /// <summary>Путь к свойству</summary>
    public string PropertyPath { get; }

    /// <summary>Доступные значения для выбора (для Enum/String фильтров)</summary>
    public ObservableCollection<FilterValueItem> AvailableValues { get; } = [];

    /// <summary>Минимальное значение (для Range фильтров)</summary>
    public object? MinValue { get; set; }

    /// <summary>Максимальное значение (для Range фильтров)</summary>
    public object? MaxValue { get; set; }

    /// <summary>Текущее минимальное значение фильтра</summary>
    [ObservableProperty]
    public partial object? CurrentMinValue { get; set; }

    /// <summary>Текущее максимальное значение фильтра</summary>
    [ObservableProperty]
    public partial object? CurrentMaxValue { get; set; }

    /// <summary>Текст для поиска (для TextSearch)</summary>
    [ObservableProperty]
    public partial string? SearchText { get; set; }

    /// <summary>Активен ли фильтр</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Функция проверки события</summary>
    [ObservableProperty]
    public partial Func<EventBase, bool> FilterPredicate { get; set; }

    public FilterDescriptor(
        string id,
        string displayName,
        FilterType filterType,
        string propertyPath,
        Func<EventBase, bool> filterPredicate,
        string category = "Общие",
        int priority = 100)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        FilterType = filterType;
        PropertyPath = propertyPath;
        FilterPredicate = filterPredicate ?? throw new ArgumentNullException(nameof(filterPredicate));
        Category = category;
        Priority = priority;
    }

    /// <summary>
    /// Обновляет предикат фильтра (для Range-фильтров).
    /// </summary>
    public void UpdatePredicate(Func<EventBase, bool> newPredicate)
    {
        FilterPredicate = newPredicate ?? throw new ArgumentNullException(nameof(newPredicate));
    }

    /// <summary>
    /// Сбрасывает фильтр к начальному состоянию
    /// </summary>
    public void Reset()
    {
        IsActive = false;
        CurrentMinValue = MinValue;
        CurrentMaxValue = MaxValue;
        SearchText = null;

        foreach (var value in AvailableValues)
            value.IsSelected = false;
    }
}