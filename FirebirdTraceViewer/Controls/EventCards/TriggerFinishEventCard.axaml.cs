using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceViewer.Controls.EventCards;

public class TriggerFinishEventCard : TemplatedControl
{
    
    private Button? _copyButton;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Отписываемся если шаблон переинициализировался
        if (_copyButton != null)
            _copyButton.Click -= CopyButtonOnClick;

        _copyButton = e.NameScope.Find<Button>("PART_CopyTriggerButton");

        if (_copyButton != null)
            _copyButton.Click += CopyButtonOnClick;
    }

    private async void CopyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard == null)
            return;

        await topLevel.Clipboard.SetTextAsync(TriggerName);

        Console.WriteLine($"Copied: {TriggerName}");
    }
    
    public static readonly StyledProperty<DateTime> TimestampProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, DateTime>(nameof(Timestamp), DateTime.MinValue);
    
    public static readonly StyledProperty<int> TraceIdProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, int>(nameof(TraceId), 0);
    
    public static readonly StyledProperty<string> HexTraceIdProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(HexTraceId), "0");
    
    public static readonly StyledProperty<string> DatabasePathProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(DatabasePath), "database_path");
    
    public static readonly StyledProperty<string> UserProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(User), "user_name");
    
    public static readonly StyledProperty<string> RoleProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(Role), "role_name");
    
    public static readonly StyledProperty<int> AttachmentIdProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, int>(nameof(AttachmentId), 0);
    
    public static readonly StyledProperty<string> ProtocolProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(Protocol), "TCPv4");
    
    public static readonly StyledProperty<string> AddressProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(Address), "192.168.3.5");
    
    public static readonly StyledProperty<int> PortProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, int>(nameof(Port), 3050);
    
    public static readonly StyledProperty<string> CharsetProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(Charset), "WIN1251");
    
    public static readonly StyledProperty<string> ProcessPathProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(ProcessPath), "C:/Program/App.exe");
    
    public static readonly StyledProperty<int> ProcessIdProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, int>(nameof(ProcessId), 12341);
    
    public static readonly StyledProperty<int> TransactionIdProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, int>(nameof(TransactionId), 12341);
    
    public static readonly StyledProperty<string> IsolationLevelProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(IsolationLevel), "READ_COMMITTED");
    
    public static readonly StyledProperty<string> ConsistencyModeProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(ConsistencyMode), "READ_CONSISTENCY");
    
    public static readonly StyledProperty<string> LockModeProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(LockMode), "NOWAIT");
    
    public static readonly StyledProperty<string> AccessModeProperty =
        AvaloniaProperty.Register<TriggerFinishEventCard, string>(nameof(AccessMode), "READ_ONLY");
    
    public static readonly StyledProperty<string> TriggerNameProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, string>(nameof(TriggerName), "KPK$SERVCHAINSHED_BU0");
    
    public static readonly StyledProperty<string> TableProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, string>(nameof(Table), "KPK$SERVCHAINSHED");
    
    public static readonly StyledProperty<string> TimingProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, string>(nameof(Timing), "BEFORE");
    
    public static readonly StyledProperty<string> EventProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, string>(nameof(Event), "UPDATE");
    
    public static readonly StyledProperty<int> ExecuteMsProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, int>(nameof(ExecuteMs), 0);
    
    public static readonly StyledProperty<int> FetchCountProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, int>(nameof(FetchCount), 0);
    
    public static readonly StyledProperty<int> ReadCountProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, int>(nameof(ReadCount), 0);
    
    public static readonly StyledProperty<int> WriteCountProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, int>(nameof(WriteCount), 0);
    
    public static readonly StyledProperty<int> MarkCountProperty =
        AvaloniaProperty.Register<TriggerStartEventCard, int>(nameof(MarkCount), 0);
    
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
    
    public int TransactionId
    {
        get => GetValue(TransactionIdProperty);
        set => SetValue(TransactionIdProperty, value);
    }
 
    public string IsolationLevel
    {
        get => GetValue(IsolationLevelProperty);
        set => SetValue(IsolationLevelProperty, value);
    }
    
    public string ConsistencyMode
    {
        get => GetValue(ConsistencyModeProperty);
        set => SetValue(ConsistencyModeProperty, value);
    }
    
    public string LockMode
    {
        get => GetValue(LockModeProperty);
        set => SetValue(LockModeProperty, value);
    }
    
    public string AccessMode
    {
        get => GetValue(AccessModeProperty);
        set => SetValue(AccessModeProperty, value);
    }
    
    public string TriggerName
    {
        get => GetValue(TriggerNameProperty);
        set => SetValue(TriggerNameProperty, value);
    }
    
    public string Table
    {
        get => GetValue(TableProperty);
        set => SetValue(TableProperty, value);
    }
    
    public string Timing
    {
        get => GetValue(TimingProperty);
        set => SetValue(TimingProperty, value);
    }
    
    public string Event
    {
        get => GetValue(EventProperty);
        set => SetValue(EventProperty, value);
    }
    
    public int ExecuteMs
    {
        get => GetValue(ExecuteMsProperty);
        set => SetValue(ExecuteMsProperty, value);
    }
    
    public int FetchCount
    {
        get => GetValue(FetchCountProperty);
        set => SetValue(FetchCountProperty, value);
    }
    
    public int ReadCount
    {
        get => GetValue(ReadCountProperty);
        set => SetValue(ReadCountProperty, value);
    }
    
    public int WriteCount
    {
        get => GetValue(WriteCountProperty);
        set => SetValue(WriteCountProperty, value);
    }
    
    public int MarkCount
    {
        get => GetValue(MarkCountProperty);
        set => SetValue(MarkCountProperty, value);
    }
    
}