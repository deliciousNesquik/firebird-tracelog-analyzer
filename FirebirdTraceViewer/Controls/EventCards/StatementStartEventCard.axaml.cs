using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceViewer.Controls.EventCards;

public class StatementStartEventCard : TemplatedControl
{
    
    private Button? _copyButton;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Отписываемся если шаблон переинициализировался
        if (_copyButton != null)
            _copyButton.Click -= CopyButtonOnClick;

        _copyButton = e.NameScope.Find<Button>("PART_CopySqlButton");

        if (_copyButton != null)
            _copyButton.Click += CopyButtonOnClick;
    }

    private async void CopyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard == null)
            return;

        if (Params == null || Params.Count == 0)
        {
            await topLevel.Clipboard.SetTextAsync(Sql);
            Console.WriteLine($"Copied: {Sql}");
            return;
        }

        var sql = new StringBuilder();
        int index = 0;

        foreach (var ch in Sql)
        {
            if (ch == '?' && index < Params.Count)
            {
                sql.Append(FormatParam(Params[index]));
                index++;
            }
            else
            {
                sql.Append(ch);
            }
        }

        await topLevel.Clipboard.SetTextAsync(sql.ToString());
        Console.WriteLine($"Copied: {sql.ToString()}");
    }
    
    private static string FormatParam(SqlParam param)
    {
        var value = param.Value?.ToString();

        if (value == "<NULL>")
            return "NULL";

        return param.Dtype.ToLower() switch
        {
            "varchar(32764)" or "varchar" or "char" or "text" =>
                $"'{value?.Replace("'", "''")}'",

            "timestamp" =>
                $"'{value}'",

            "date" =>
                $"'{value}'",

            "time" =>
                $"'{value}'",

            "bigint" or "int" or "smallint" or "integer" =>
                value ?? "NULL",

            _ =>
                value ?? "NULL"
        };
    }
    
    public static readonly StyledProperty<DateTime> TimestampProperty =
        AvaloniaProperty.Register<StatementStartEventCard, DateTime>(nameof(Timestamp), DateTime.MinValue);
    
    public static readonly StyledProperty<int> TraceIdProperty =
        AvaloniaProperty.Register<StatementStartEventCard, int>(nameof(TraceId), 0);
    
    public static readonly StyledProperty<string> HexTraceIdProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(HexTraceId), "0");
    
    public static readonly StyledProperty<int> StatementIdProperty =
        AvaloniaProperty.Register<StatementStartEventCard, int>(nameof(StatementId), 0);
    
    public static readonly StyledProperty<string> DatabasePathProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(DatabasePath), "database_path");
    
    public static readonly StyledProperty<string> UserProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(User), "user_name");
    
    public static readonly StyledProperty<string> RoleProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(Role), "role_name");
    
    public static readonly StyledProperty<int> AttachmentIdProperty =
        AvaloniaProperty.Register<StatementStartEventCard, int>(nameof(AttachmentId), 0);
    
    public static readonly StyledProperty<string> ProtocolProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(Protocol), "TCPv4");
    
    public static readonly StyledProperty<string> AddressProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(Address), "192.168.3.5");
    
    public static readonly StyledProperty<int> PortProperty =
        AvaloniaProperty.Register<StatementStartEventCard, int>(nameof(Port), 3050);
    
    public static readonly StyledProperty<string> CharsetProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(Charset), "WIN1251");
    
    public static readonly StyledProperty<string> ProcessPathProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(ProcessPath), "C:/Program/App.exe");
    
    public static readonly StyledProperty<int> ProcessIdProperty =
        AvaloniaProperty.Register<StatementStartEventCard, int>(nameof(ProcessId), 12341);
    
    public static readonly StyledProperty<int> TransactionIdProperty =
        AvaloniaProperty.Register<StatementStartEventCard, int>(nameof(TransactionId), 12341);
    
    public static readonly StyledProperty<string> IsolationLevelProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(IsolationLevel), "READ_COMMITTED");
    
    public static readonly StyledProperty<string> ConsistencyModeProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(ConsistencyMode), "READ_CONSISTENCY");
    
    public static readonly StyledProperty<string> LockModeProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(LockMode), "NOWAIT");
    
    public static readonly StyledProperty<string> AccessModeProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(AccessMode), "READ_ONLY");
    
    public static readonly StyledProperty<string> SqlProperty =
        AvaloniaProperty.Register<StatementStartEventCard, string>(nameof(Sql), "select * from rdb$database as rd where rd.int = 10");
    
    public static readonly StyledProperty<IReadOnlyList<SqlParam>> ParamsProperty =
        AvaloniaProperty.Register<StatementStartEventCard, IReadOnlyList<SqlParam>>(nameof(Params), new List<SqlParam>());
    
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
    
    public int StatementId
    {
        get => GetValue(StatementIdProperty);
        set => SetValue(StatementIdProperty, value);
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
    
    public string Sql
    {
        get => GetValue(SqlProperty);
        set => SetValue(SqlProperty, value);
    }
    
    public IReadOnlyList<SqlParam> Params
    {
        get => GetValue(ParamsProperty);
        set => SetValue(ParamsProperty, value);
    }
    
}