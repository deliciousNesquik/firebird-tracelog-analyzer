using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FirebirdTraceParser.Core.Models.Events;

namespace FirebirdTraceViewer.Services.Sorting;

/// <summary>
/// Описывает один вариант сортировки.
/// </summary>
public sealed class SortDescriptor : INotifyPropertyChanged
{
    private bool _isSelected;

    /// <summary>Уникальный идентификатор</summary>
    public string Id { get; }
    
    /// <summary>Отображаемое имя</summary>
    public string DisplayName { get; }
    
    /// <summary>Категория в UI</summary>
    public string Category { get; }
    
    /// <summary>Приоритет отображения</summary>
    public int Priority { get; }
    
    /// <summary>Функция сравнения событий</summary>
    public Comparison<EventBase> Comparer { get; }
    
    /// <summary>Является ли сортировкой по умолчанию</summary>
    public bool IsDefault { get; init; }

    /// <summary>Выбрана ли эта сортировка в данный момент</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public SortDescriptor(
        string id,
        string displayName,
        Comparison<EventBase> comparer,
        string category = "Общие",
        int priority = 100)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        Category = category;
        Priority = priority;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}