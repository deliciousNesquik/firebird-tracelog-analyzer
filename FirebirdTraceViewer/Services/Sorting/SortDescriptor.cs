using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.ViewModels;

namespace FirebirdTraceViewer.Services.Sorting;

/// <summary>
/// Описывает один вариант сортировки.
/// </summary>
public partial class SortDescriptor : ViewModelBase
{
    
    /// <summary>Уникальный идентификатор сортировки</summary>
    public string Id { get; }
    
    /// <summary>Отображаемое имя сортировки</summary>
    public string DisplayName { get; }
    
    /// <summary>Категория сортировки</summary>
    public string Category { get; }
    
    /// <summary>Приоритет отображения сортировки</summary>
    public int Priority { get; }
    
    /// <summary>Функция сравнения событий для сортировки</summary>
    public Func<EventBase, EventBase, bool, int> Comparer { get; }
    
    /// <summary>Является ли сортировкой по умолчанию</summary>
    public bool IsDefault { get; init; }

    /// <summary>Выбрана ли эта сортировка в данный момент</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SortDescriptor(
        string id,
        string displayName,
        Func<EventBase, EventBase, bool, int> comparer,
        bool isDefault,
        string category = "General",
        int priority = 100)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        IsDefault = isDefault;
        Category = category;
        Priority = priority;
        IsDefault = isDefault;
    }
}