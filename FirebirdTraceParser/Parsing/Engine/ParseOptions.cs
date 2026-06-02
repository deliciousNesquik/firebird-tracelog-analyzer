using System.Text;

namespace FirebirdTraceParser.Parsing.Engine;

/// <summary>
/// Опции парсинга trace логов.
/// </summary>
public sealed record ParseOptions
{
    /// <summary>Кодировка файла (по умолчанию UTF-8)</summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;
    
    /// <summary>Режим валидации блоков</summary>
    public ValidationMode ValidationMode { get; init; } = ValidationMode.Strict;
    
    /// <summary>Размер батча для потоковой обработки</summary>
    public int BatchSize { get; init; } = 256;
    
    /// <summary>Тайм-аут для regex операций</summary>
    public TimeSpan RegexTimeout { get; init; } = TimeSpan.FromSeconds(1);
    
    /// <summary>Включить парсинг таблиц производительности</summary>
    public bool ParsePerformanceTables { get; init; } = true;
    
    public static ParseOptions Default => new();
}