using FirebirdTraceParser.Core.Attributes;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о подключении к базе данных Firebird.
/// Соответствует Python AttachmentInfo.
/// </summary>
public sealed record AttachmentInfo
{
    public required string DatabasePath { get; init; }
    
    [SortableField("ID подключения", Priority = 9, Category = "Подключение")]
    public required int AttachmentId { get; init; }
    
    [SortableField("Пользователь", Priority = 10, Category = "Подключение")]
    public required string User { get; init; }
    
    [SortableField("Роль", Priority = 11, Category = "Подключение")]
    public required string Role { get; init; }
    
    [SortableField("Кодировка", Priority = 12, Category = "Подключение")]
    public required string Charset { get; init; }
    
    [SortableField("Протокол", Priority = 13, Category = "Подключение")]
    public required string Protocol { get; init; }
    
    [SortableField("Адрес", Priority = 14, Category = "Подключение")]
    public required string Address { get; init; }
    public required int Port { get; init; }
    
    /// <summary>Путь к исполняемому файлу клиента (опционально)</summary>
    public string? ProcessPath { get; init; }
    
    /// <summary>ID процесса клиента (опционально)</summary>
    public int? ProcessId { get; init; }
}