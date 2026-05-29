using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FirebirdTraceViewer.ViewModels;

namespace FirebirdTraceViewer.Services.Filtering;

/// <summary>
/// Представляет одно значение для фильтра (например, пункт в чекбоксе).
/// </summary>
public partial class FilterValueItem : ViewModelBase
{
    
    /// <summary>Внутреннее значение (например, enum или строка)</summary>
    public object Value { get; }
    
    /// <summary>Отображаемое имя</summary>
    public string DisplayName { get; }
    
    /// <summary>Количество событий с этим значением</summary>
    public int Count { get; set; }
    
    /// <summary>Выбрано ли значение</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public FilterValueItem(object value, string displayName, int count = 0)
    {
        Value = value;
        DisplayName = displayName;
        Count = count;
    }
}