using FirebirdTraceParser.Core.Attributes;
using FirebirdTraceParser.Core.Parsing.Utils;

namespace FirebirdTraceParser.Core.Models.ValueObjects;

/// <summary>
/// Информация о транзакции Firebird.
/// Соответствует Python TransactionInfo.
/// </summary>
public sealed record TransactionInfo
{
    [SortableField("ID транзакции", Priority = 20, Category = "Транзакция")]
    [FilterableField("ID транзакции", Category = "Транзакция", FilterType =  FilterType.StringMultiSelect)]
    public required int TransactionId { get; init; }
    
    private string _isolationLevel = string.Empty;
    /// <summary>Уровень изоляции (READ_COMMITTED, SNAPSHOT, etc.)</summary>
    [SortableField("Уровень изоляции", Priority = 21, Category = "Транзакция")]
    [FilterableField("Уровень изоляции", Category = "Транзакция", FilterType =  FilterType.StringMultiSelect)]
    public required string IsolationLevel
    {
        get => _isolationLevel;
        init => _isolationLevel = StringPool.Intern(value);
    }
    
    
    private string _consistencyMode = string.Empty;
    /// <summary>Режим консистентности (READ_CONSISTENCY, etc.)</summary>
    [SortableField("Уровень консистентности", Priority = 22, Category = "Транзакция")]
    [FilterableField("Уровень консистентности", Category = "Транзакция", FilterType =  FilterType.StringMultiSelect)]
    public required string ConsistencyMode
    {
        get => _consistencyMode;
        init => _consistencyMode = StringPool.Intern(value);
    }

    private string _lockMode = string.Empty;
    /// <summary>Режим блокировки (WAIT, NOWAIT)</summary>
    [SortableField("Уровень блокировки", Priority = 23, Category = "Транзакция")]
    [FilterableField("Уровень блокировки", Category = "Транзакция", FilterType =  FilterType.StringMultiSelect)]
    public required string LockMode
    {
        get => _lockMode;
        init => _lockMode = StringPool.Intern(value);
    }
    
    private string _accessMode = string.Empty;
    /// <summary>Режим доступа (READ_WRITE, READ_ONLY)</summary>
    [SortableField("Уровень доступа", Priority = 24, Category = "Транзакция")]
    [FilterableField("Уровень доступа", Category = "Транзакция", FilterType =  FilterType.StringMultiSelect)]
    public required string AccessMode
    {
        get => _accessMode;
        init => _accessMode = StringPool.Intern(value);
    }
}