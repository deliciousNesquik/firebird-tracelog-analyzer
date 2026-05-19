using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace FirebirdTraceViewer.Controls.EventCards;

public class AttachDatabaseEventCard : TemplatedControl
{
    public static readonly StyledProperty<DateTime> TimestampProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, DateTime>(nameof(Timestamp), DateTime.MinValue);
    
    public static readonly StyledProperty<int> TraceIdProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, int>(nameof(TraceId), 0);
    
    public static readonly StyledProperty<string> HexTraceIdProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(HexTraceId), "0");
    
    public static readonly StyledProperty<string> DatabasePathProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(DatabasePath), "database_path");
    
    public static readonly StyledProperty<string> UserProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(User), "user_name");
    
    public static readonly StyledProperty<string> RoleProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(Role), "role_name");
    
    public static readonly StyledProperty<int> AttachmentIdProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, int>(nameof(AttachmentId), 0);
    
    public static readonly StyledProperty<string> ProtocolProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(Protocol), "TCPv4");
    
    public static readonly StyledProperty<string> AddressProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(Address), "192.168.3.5");
    
    public static readonly StyledProperty<int> PortProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, int>(nameof(Port), 3050);
    
    public static readonly StyledProperty<string> CharsetProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(Charset), "WIN1251");
    
    public static readonly StyledProperty<string> ProcessPathProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, string>(nameof(ProcessPath), "C:/Program/App.exe");
    
    public static readonly StyledProperty<int> ProcessIdProperty =
        AvaloniaProperty.Register<AttachDatabaseEventCard, int>(nameof(ProcessId), 12341);
    
    
    public DateTime Timestamp
    {
        get => GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }
    
    public int TraceId
    {
        get => GetValue(TraceIdProperty);
        set => SetValue(TraceIdProperty, value);
    }
    
    public string HexTraceId
    {
        get => GetValue(HexTraceIdProperty);
        set => SetValue(HexTraceIdProperty, value);
    }
    
    public string DatabasePath
    {
        get => GetValue(DatabasePathProperty);
        set => SetValue(DatabasePathProperty, value);
    }
    
    public string User
    {
        get => GetValue(UserProperty);
        set => SetValue(UserProperty, value);
    }
    
    public string Role
    {
        get => GetValue(RoleProperty);
        set => SetValue(RoleProperty, value);
    }
    
    public int AttachmentId
    {
        get => GetValue(AttachmentIdProperty);
        set => SetValue(AttachmentIdProperty, value);
    }
    
    public string Protocol
    {
        get => GetValue(ProtocolProperty);
        set => SetValue(ProtocolProperty, value);
    }
    
    public string Address
    {
        get => GetValue(AddressProperty);
        set => SetValue(AddressProperty, value);
    }
    
    public int Port
    {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }
    
    public string Charset
    {
        get => GetValue(CharsetProperty);
        set => SetValue(CharsetProperty, value);
    }
    
    public string ProcessPath
    {
        get => GetValue(ProcessPathProperty);
        set => SetValue(ProcessPathProperty, value);
    }
    
    public int ProcessId
    {
        get => GetValue(ProcessIdProperty);
        set => SetValue(ProcessIdProperty, value);
    }
}