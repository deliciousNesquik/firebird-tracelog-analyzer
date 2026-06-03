namespace FirebirdTraceAnalyzer.Enums;

/// <summary>
/// Метод аутентификации SSH
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>Аутентификация по паролю</summary>
    Password,
    
    /// <summary>Аутентификация по приватному ключу</summary>
    PrivateKey
}