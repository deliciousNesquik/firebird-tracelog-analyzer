namespace FirebirdTraceAnalyzer.Enums;

/// <summary>
/// Статус SSH подключения
/// </summary>
public enum ConnectionStatus
{
    /// <summary>Не подключен</summary>
    Disconnected,
    
    /// <summary>Подключение...</summary>
    Connecting,
    
    /// <summary>Подключен</summary>
    Connected,
    
    /// <summary>Ошибка подключения</summary>
    Error,
    
    /// <summary>Аутентификация...</summary>
    Authenticating,
    
    /// <summary>Получение списка файлов...</summary>
    FetchingFiles,
    
    /// <summary>Загрузка файлов...</summary>
    Downloading
}