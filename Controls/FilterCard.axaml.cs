using Avalonia;
using Avalonia.Controls.Primitives;

namespace FTV.Controls;

public class FilterCard : TemplatedControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<FilterCard, string>(nameof(Title), "");
    
    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<FilterCard, string>(nameof(Value), "");

    
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}