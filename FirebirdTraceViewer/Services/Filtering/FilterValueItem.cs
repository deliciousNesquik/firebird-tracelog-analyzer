using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FirebirdTraceViewer.Services.Filtering;

/// <summary>
/// Представляет одно значение для фильтра (например, пункт в чекбоксе).
/// </summary>
public sealed class FilterValueItem : INotifyPropertyChanged
{
    private bool _isSelected;
    
    /// <summary>Внутреннее значение (например, enum или строка)</summary>
    public object Value { get; }
    
    /// <summary>Отображаемое имя</summary>
    public string DisplayName { get; }
    
    /// <summary>Количество событий с этим значением</summary>
    public int Count { get; set; }
    
    /// <summary>Выбрано ли значение</summary>
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

    public FilterValueItem(object value, string displayName, int count = 0)
    {
        Value = value;
        DisplayName = displayName;
        Count = count;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}