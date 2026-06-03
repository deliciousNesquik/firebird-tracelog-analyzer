using System.Security.Cryptography;
using System.Text;
using FirebirdTraceAnalyzer.Interfaces;
using NLog;

namespace FirebirdTraceAnalyzer.Services;

/// <summary>
/// Сервис для безопасного хранения учётных данных
/// Использует ProtectedData для шифрования на Windows
/// Для Linux/macOS использует простое файловое хранилище (можно улучшить)
/// </summary>
public class CredentialStorageService : ICredentialStorageService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string _storageDirectory;

    public CredentialStorageService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "FirebirdTraceAnalyzer", "Credentials");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
            Logger.Info("Created credentials storage directory: {Path}", _storageDirectory);
        }
    }

    public Task SavePasswordAsync(string server, string username, string password)
    {
        return Task.Run(() =>
        {
            try
            {
                var key = CreateKey(server, username);
                var encryptedPassword = EncryptPassword(password);
                var filePath = GetCredentialFilePath(key);

                File.WriteAllText(filePath, encryptedPassword);
                
                Logger.Info("Password saved for {Username}@{Server}", username, server);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving password");
                throw;
            }
        });
    }

    public Task<string?> GetPasswordAsync(string server, string username)
    {
        return Task.Run<string?>(() =>
        {
            try
            {
                var key = CreateKey(server, username);
                var filePath = GetCredentialFilePath(key);

                if (!File.Exists(filePath))
                {
                    Logger.Debug("No saved password found for {Username}@{Server}", username, server);
                    return null;
                }

                var encryptedPassword = File.ReadAllText(filePath);
                var password = DecryptPassword(encryptedPassword);

                Logger.Debug("Password retrieved for {Username}@{Server}", username, server);
                return password;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error retrieving password");
                return null;
            }
        });
    }

    public Task DeletePasswordAsync(string server, string username)
    {
        return Task.Run(() =>
        {
            try
            {
                var key = CreateKey(server, username);
                var filePath = GetCredentialFilePath(key);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.Info("Password deleted for {Username}@{Server}", username, server);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error deleting password");
                throw;
            }
        });
    }

    public Task<bool> HasPasswordAsync(string server, string username)
    {
        return Task.Run(() =>
        {
            var key = CreateKey(server, username);
            var filePath = GetCredentialFilePath(key);
            return File.Exists(filePath);
        });
    }

    private static string CreateKey(string server, string username)
    {
        var combined = $"{server}:{username}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash);
    }

    private string GetCredentialFilePath(string key)
    {
        return Path.Combine(_storageDirectory, $"{key}.cred");
    }

    private static string EncryptPassword(string password)
    {
        if (OperatingSystem.IsWindows())
        {
            // Используем ProtectedData на Windows
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
                passwordBytes,
                null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            
            return Convert.ToBase64String(encryptedBytes);
        }
        else
        {
            // Для Linux/macOS используем простое base64 (небезопасно, но работает)
            // TODO: Интеграция с системным хранилищем (keyring на Linux, Keychain на macOS)
            Logger.Warn("Running on non-Windows OS - using basic encoding (not secure)");
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(passwordBytes);
        }
    }

    private static string DecryptPassword(string encryptedPassword)
    {
        if (OperatingSystem.IsWindows())
        {
            var encryptedBytes = Convert.FromBase64String(encryptedPassword);
            var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes,
                null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        else
        {
            var passwordBytes = Convert.FromBase64String(encryptedPassword);
            return Encoding.UTF8.GetString(passwordBytes);
        }
    }
}