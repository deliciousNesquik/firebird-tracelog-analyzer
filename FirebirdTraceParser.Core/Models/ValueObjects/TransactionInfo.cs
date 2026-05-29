using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о транзакции Firebird.
/// Соответствует Python TransactionInfo.
/// </summary>
public sealed record TransactionInfo
{
    [SortableField("Transaction ID", Priority = 20, Category = "Transaction")]
    [FilterableField("Transaction ID", Category = "Transaction", FilterType =  FilterType.StringMultiSelect)]
    public required int TransactionId { get; init; }
    
    /// <summary>Уровень изоляции (READ_COMMITTED, SNAPSHOT, etc.)</summary>
    [SortableField("Isolation level", Priority = 21, Category = "Transaction")]
    [FilterableField("Isolation level", Category = "Transaction", FilterType =  FilterType.StringMultiSelect)]
    public required string IsolationLevel { get; init; }
    
    
    /// <summary>Режим консистентности (READ_CONSISTENCY, etc.)</summary>
    [SortableField("Consistency mode", Priority = 22, Category = "Transaction")]
    [FilterableField("Consistency mode", Category = "Transaction", FilterType =  FilterType.StringMultiSelect)]
    public required string ConsistencyMode { get; init; }
    
    /// <summary>Режим блокировки (WAIT, NOWAIT)</summary>
    [SortableField("Lock mode", Priority = 23, Category = "Transaction")]
    [FilterableField("Lock mode", Category = "Transaction", FilterType =  FilterType.StringMultiSelect)]
    public required string LockMode { get; init; }
    
    /// <summary>Режим доступа (READ_WRITE, READ_ONLY)</summary>
    [SortableField("Access mode", Priority = 24, Category = "Transaction")]
    [FilterableField("Access mode", Category = "Transaction", FilterType =  FilterType.StringMultiSelect)]
    public required string AccessMode { get; init; }
}