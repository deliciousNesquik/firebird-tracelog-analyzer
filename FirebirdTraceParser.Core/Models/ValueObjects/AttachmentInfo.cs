namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о подключении к базе данных Firebird.
/// Соответствует Python AttachmentInfo.
/// </summary>
public sealed record AttachmentInfo
{
    public required string DatabasePath { get; init; }
    public required int AttachmentId { get; init; }
    public required string User { get; init; }
    public required string Role { get; init; }
    public required string Charset { get; init; }
    public required string Protocol { get; init; }
    public required string Address { get; init; }
    public required int Port { get; init; }
    
    /// <summary>Путь к исполняемому файлу клиента (опционально)</summary>
    public string? ProcessPath { get; init; }
    
    /// <summary>ID процесса клиента (опционально)</summary>
    public int? ProcessId { get; init; }
}