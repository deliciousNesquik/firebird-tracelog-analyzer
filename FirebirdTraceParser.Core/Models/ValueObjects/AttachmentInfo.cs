using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о подключении к базе данных Firebird.
/// Соответствует Python AttachmentInfo.
/// </summary>
public sealed record AttachmentInfo
{
    
    private string _databasePath = string.Empty;
    [FilterableField("Путь до БД", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required string DatabasePath
    {
        get => _databasePath;
        init => _databasePath = StringPool.Intern(value);
    }

    [SortableField("ID подключения", Priority = 9, Category = "Подключение")]
    [FilterableField("ID подключения", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required int AttachmentId { get; init; }
    
    private string _user = string.Empty;
    [SortableField("Пользователь", Priority = 10, Category = "Подключение")]
    [FilterableField("Пользователь", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required string User
    {
        get => _user; 
        init => _user = StringPool.Intern(value);
    }
    
    private string _role = string.Empty;

    [SortableField("Роль", Priority = 11, Category = "Подключение")]
    [FilterableField("Роль", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required string Role
    {
        get => _role;
        init => _role = StringPool.Intern(value);
    }

    private string _charset = string.Empty;

    [SortableField("Кодировка", Priority = 12, Category = "Подключение")]
    [FilterableField("Кодировка", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required string Charset
    {
        get => _charset;
        init => _charset = StringPool.Intern(value);
    }

    private string _protocol = string.Empty;

    [SortableField("Протокол", Priority = 13, Category = "Подключение")]
    [FilterableField("Протокол", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required string Protocol
    {
        get => _protocol;
        init => _protocol = StringPool.Intern(value);
    }
    
    private string _address = string.Empty;

    [SortableField("Адрес", Priority = 14, Category = "Подключение")]
    [FilterableField("Адрес", Category = "Подключение", FilterType =  FilterType.StringMultiSelect)]
    public required string Address
    {
        get => _address;
        init => _address = StringPool.Intern(value);
    }
    public required int Port { get; init; }

    private string _processPath = string.Empty;

    /// <summary>Путь к исполняемому файлу клиента (опционально)</summary>
    public string? ProcessPath
    {
        get => _processPath;
        init => _processPath = StringPool.Intern(value);
    }
    
    /// <summary>ID процесса клиента (опционально)</summary>
    public int? ProcessId { get; init; }
}