namespace FirebirdTraceParser.Parsing.Engine;

/// <summary>
/// Режим валидации при парсинге.
/// </summary>
public enum ValidationMode
{
    /// <summary>Строгий режим: любая ошибка парсинга блока - warning</summary>
    Strict,
    
    /// <summary>Мягкий режим: пропускаются только критические ошибки</summary>
    Relaxed
}