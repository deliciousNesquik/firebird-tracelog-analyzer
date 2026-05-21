using FirebirdTraceParser.Core.Attributes;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о транзакции Firebird.
/// Соответствует Python TransactionInfo.
/// </summary>
public sealed record TransactionInfo
{
    [SortableField("ID транзакции", Priority = 20, Category = "Транзакция")]
    public required int TransactionId { get; init; }
    
    /// <summary>Уровень изоляции (READ_COMMITTED, SNAPSHOT, etc.)</summary>
    [SortableField("Уровень изоляции", Priority = 21, Category = "Транзакция")]
    public required string IsolationLevel { get; init; }
    
    /// <summary>Режим консистентности (READ_CONSISTENCY, etc.)</summary>
    [SortableField("Уровень консистентности", Priority = 22, Category = "Транзакция")]
    public required string ConsistencyMode { get; init; }
    
    /// <summary>Режим блокировки (WAIT, NOWAIT)</summary>
    [SortableField("Уровень блокировки", Priority = 23, Category = "Транзакция")]
    public required string LockMode { get; init; }
    
    /// <summary>Режим доступа (READ_WRITE, READ_ONLY)</summary>
    [SortableField("Уровень доступа", Priority = 24, Category = "Транзакция")]
    public required string AccessMode { get; init; }
}