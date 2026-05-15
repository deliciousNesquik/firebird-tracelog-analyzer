namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о транзакции Firebird.
/// Соответствует Python TransactionInfo.
/// </summary>
public sealed record TransactionInfo
{
    public required int TransactionId { get; init; }
    
    /// <summary>Уровень изоляции (READ_COMMITTED, SNAPSHOT, etc.)</summary>
    public required string IsolationLevel { get; init; }
    
    /// <summary>Режим консистентности (READ_CONSISTENCY, etc.)</summary>
    public required string ConsistencyMode { get; init; }
    
    /// <summary>Режим блокировки (WAIT, NOWAIT)</summary>
    public required string LockMode { get; init; }
    
    /// <summary>Режим доступа (READ_WRITE, READ_ONLY)</summary>
    public required string AccessMode { get; init; }
}