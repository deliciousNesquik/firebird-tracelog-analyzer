using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace FTV.Controls;

public class FocusTextBlock : TemplatedControl
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<FilterCard, string>(nameof(Header), "");
    
    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<FilterCard, string>(nameof(Value), "");
 
    
    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}