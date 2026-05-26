using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Models.Events;

namespace FirebirdTraceViewer.Services.Filtering;

/// <summary>
/// Описывает один фильтр со всеми его настройками.
/// </summary>
public sealed class FilterDescriptor : INotifyPropertyChanged
{
    private bool _isActive;
    private Func<EventBase, bool> _filterPredicate;
    private object? _currentMinValue;
    private object? _currentMaxValue;
    
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
    public object? CurrentMinValue
    {
        get => _currentMinValue;
        set
        {
            if (!Equals(_currentMinValue, value))
            {
                _currentMinValue = value;
                OnPropertyChanged();
                OnRangeValueChanged(); // ← вызываем обновление предиката
            }
        }
    }

    /// <summary>Текущее максимальное значение фильтра</summary>
    public object? CurrentMaxValue
    {
        get => _currentMaxValue;
        set
        {
            if (!Equals(_currentMaxValue, value))
            {
                _currentMaxValue = value;
                OnPropertyChanged();
                OnRangeValueChanged(); // ← вызываем обновление предиката
            }
        }
    }
    
    /// <summary>Текст для поиска (для TextSearch)</summary>
    public string? SearchText { get; set; }
    
    /// <summary>Активен ли фильтр</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }
    
    /// <summary>Событие изменения диапазона (для подписки ViewModel)</summary>
    public event EventHandler? RangeValueChanged;

    private void OnRangeValueChanged()
    {
        RangeValueChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>Функция проверки события</summary>
    public Func<EventBase, bool> FilterPredicate => _filterPredicate;

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
        _filterPredicate = filterPredicate ?? throw new ArgumentNullException(nameof(filterPredicate));
        Category = category;
        Priority = priority;
    }

    /// <summary>
    /// Обновляет предикат фильтра (для Range-фильтров).
    /// </summary>
    public void UpdatePredicate(Func<EventBase, bool> newPredicate)
    {
        _filterPredicate = newPredicate ?? throw new ArgumentNullException(nameof(newPredicate));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}