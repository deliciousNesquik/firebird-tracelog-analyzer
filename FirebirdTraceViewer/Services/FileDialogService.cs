using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using FirebirdTraceViewer.Interfaces;

namespace FirebirdTraceViewer.Services;

public class FileDialogService : IFileDialogService
{
    public async Task<IReadOnlyList<IStorageFile>> OpenTraceFilesAsync()
    {
        // Получаем главное окно динамически
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            return Array.Empty<IStorageFile>();
        }

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файлы логов трассировки",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Trace Logs")
                {
                    Patterns = new[] { "*.log", "*.txt" }
                },
                FilePickerFileTypes.All
            }
        });

        return files;
    }

    /// <summary>
    /// Получает главное окно приложения из текущего ApplicationLifetime.
    /// </summary>
    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}