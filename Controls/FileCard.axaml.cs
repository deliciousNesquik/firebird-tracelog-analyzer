using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace FTV.Controls;

public class FileCard : TemplatedControl
{
    public static readonly StyledProperty<string> FileNameProperty =
        AvaloniaProperty.Register<FilterCard, string>(nameof(FileName), "");
    
    public static readonly StyledProperty<DateTime> StartTraceProperty =
        AvaloniaProperty.Register<FilterCard, DateTime>(nameof(StartTrace), DateTime.Now);
    
    public static readonly StyledProperty<DateTime> EndTraceProperty =
        AvaloniaProperty.Register<FilterCard, DateTime>(nameof(EndTrace), DateTime.Now);
    
    public static readonly StyledProperty<long> FileSizeProperty =
        AvaloniaProperty.Register<FilterCard, long>(nameof(FileSize), 0);
    
    public static readonly StyledProperty<long> EventCountProperty =
        AvaloniaProperty.Register<FilterCard, long>(nameof(EventCount), 0);
    
    public string FileName
    {
        get => GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }
    
    public DateTime StartTrace
    {
        get => GetValue(StartTraceProperty);
        set => SetValue(StartTraceProperty, value);
    }
    
    public DateTime EndTrace
    {
        get => GetValue(EndTraceProperty);
        set => SetValue(EndTraceProperty, value);
    }
    
    public long FileSize
    {
        get => GetValue(FileSizeProperty);
        set => SetValue(FileSizeProperty, value);
    }
    
    public long EventCount
    {
        get => GetValue(EventCountProperty);
        set => SetValue(EventCountProperty, value);
    }
}