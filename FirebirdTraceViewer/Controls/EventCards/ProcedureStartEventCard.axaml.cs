using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceViewer.Controls.EventCards;

public class ProcedureStartEventCard : TemplatedControl
{
    private Button? _copyButton;
    private Button? _copyButtonWithParams;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Отписываемся если шаблон переинициализировался
        if (_copyButton != null)
            _copyButton.Click -= CopyButtonOnClick;

        _copyButton = e.NameScope.Find<Button>("PART_CopyProcedureButton");
        _copyButtonWithParams = e.NameScope.Find<Button>("PART_CopyProcedureWithParamsButton");

        if (_copyButton != null)
            _copyButton.Click += CopyButtonOnClick;
        
        if (_copyButtonWithParams != null)
            _copyButtonWithParams.Click += CopyButtonWithParamsOnClick;
    }

    private async void CopyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard == null)
            return;

        await topLevel.Clipboard.SetTextAsync(ProcedureName);

        Console.WriteLine($"Copied: {ProcedureName}");
    }
    
    private async void CopyButtonWithParamsOnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard == null)
            return;

        var execute = new StringBuilder();

        execute.Append($"EXECUTE PROCEDURE {ProcedureName}(");

        execute.Append(string.Join(", ", Params.Select(param =>
        {
            var value = param.Value?.ToString();

            if (value == "<NULL>")
                return "NULL";

            if (int.TryParse(value, out _) ||
                decimal.TryParse(value, out _) ||
                bool.TryParse(value, out _))
            {
                return value;
            }

            return $"'{value?.Replace("'", "''")}'";
        })));

        execute.Append(')');
        
        await topLevel.Clipboard.SetTextAsync(execute.ToString());

        Console.WriteLine($"Copied: {execute.ToString()}");
    }
    
    public static readonly StyledProperty<DateTime> TimestampProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, DateTime>(nameof(Timestamp), DateTime.MinValue);
    
    public static readonly StyledProperty<int> TraceIdProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, int>(nameof(TraceId), 0);
    
    public static readonly StyledProperty<string> HexTraceIdProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(HexTraceId), "0");
    
    public static readonly StyledProperty<string> DatabasePathProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(DatabasePath), "database_path");
    
    public static readonly StyledProperty<string> UserProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(User), "user_name");
    
    public static readonly StyledProperty<string> RoleProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(Role), "role_name");
    
    public static readonly StyledProperty<int> AttachmentIdProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, int>(nameof(AttachmentId), 0);
    
    public static readonly StyledProperty<string> ProtocolProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(Protocol), "TCPv4");
    
    public static readonly StyledProperty<string> AddressProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(Address), "192.168.3.5");
    
    public static readonly StyledProperty<int> PortProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, int>(nameof(Port), 3050);
    
    public static readonly StyledProperty<string> CharsetProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(Charset), "WIN1251");
    
    public static readonly StyledProperty<string> ProcessPathProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(ProcessPath), "C:/Program/App.exe");
    
    public static readonly StyledProperty<int> ProcessIdProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, int>(nameof(ProcessId), 12341);
    
    public static readonly StyledProperty<int> TransactionIdProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, int>(nameof(TransactionId), 12341);
    
    public static readonly StyledProperty<string> IsolationLevelProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(IsolationLevel), "READ_COMMITTED");
    
    public static readonly StyledProperty<string> ConsistencyModeProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(ConsistencyMode), "READ_CONSISTENCY");
    
    public static readonly StyledProperty<string> LockModeProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(LockMode), "NOWAIT");
    
    public static readonly StyledProperty<string> AccessModeProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(AccessMode), "READ_ONLY");
    
    public static readonly StyledProperty<string> ProcedureNameProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, string>(nameof(ProcedureName), "SET_DOCPAY_AUTO_TORG2_NEW");
    
    public static readonly StyledProperty<IReadOnlyList<SqlParam>> ParamsProperty =
        AvaloniaProperty.Register<ProcedureStartEventCard, IReadOnlyList<SqlParam>>(nameof(Params), new List<SqlParam>());
    
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
    
    public string ProcedureName
    {
        get => GetValue(ProcedureNameProperty);
        set => SetValue(ProcedureNameProperty, value);
    }
    
    public IReadOnlyList<SqlParam> Params
    {
        get => GetValue(ParamsProperty);
        set => SetValue(ParamsProperty, value);
    }
    
}