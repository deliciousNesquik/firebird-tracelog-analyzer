using System.ComponentModel;

namespace FirebirdTraceParser.Core.Models.Enums;

/// <summary>
/// Типы событий трассировки Firebird Database.
/// </summary>
public enum EventType
{
    /// <summary>Инициализация trace‑сессии</summary>
    [Description("TRACE_INIT")]
    TraceInit,
    
    /// <summary>Завершение trace‑сессии</summary>
    [Description("TRACE_FINI")]
    TraceFinish,
    
    /// <summary>Подключение к базе данных</summary>
    [Description("ATTACH_DATABASE")]
    AttachDatabase,
    
    /// <summary>Отключение от базы данных</summary>
    [Description("DETACH_DATABASE")]
    DetachDatabase,
    
    /// <summary>Начало выполнения statement</summary>
    [Description("EXECUTE_STATEMENT_START")]
    ExecuteStatementStart,
    
    /// <summary>Завершение выполнения statement</summary>
    [Description("EXECUTE_STATEMENT_FINISH")]
    ExecuteStatementFinish,
    
    /// <summary>Начало выполнения процедуры</summary>
    [Description("EXECUTE_PROCEDURE_START")]
    ExecuteProcedureStart,
    
    /// <summary>Завершение выполнения процедуры</summary>
    [Description("EXECUTE_PROCEDURE_FINISH")]
    ExecuteProcedureFinish,
    
    /// <summary>Начало выполнения триггера</summary>
    [Description("EXECUTE_TRIGGER_START")]
    ExecuteTriggerStart,
    
    /// <summary>Завершение выполнения триггера</summary>
    [Description("EXECUTE_TRIGGER_FINISH")]
    ExecuteTriggerFinish,
    
    /// <summary>Ошибка завершения выполнения statement</summary>
    [Description("FAILED EXECUTE_STATEMENT_FINISH")]
    FailedExecuteStatementFinish,
    
    /// <summary>Ошибка завершения выполнения процедуры</summary>
    [Description("FAILED EXECUTE_PROCEDURE_FINISH")]
    FailedExecuteProcedureFinish,
    
    /// <summary>Ошибка завершения выполнения триггера</summary>
    [Description("FAILED EXECUTE_TRIGGER_FINISH")]
    FailedExecuteTriggerFinish,
    
    /// <summary>Ошибка ERROR AT JResultSet::fetchNext</summary>
    [Description("ERROR AT JR")]
    ErrorAtJr,
    
    /// <summary>Ошибка ERROR AT JStatement::execute/openCursor</summary>
    [Description("ERROR AT JS")]
    ErrorAtJs
}