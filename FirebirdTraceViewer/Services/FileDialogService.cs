using Avalonia.Platform.Storage;
using FirebirdTraceViewer.Interfaces;
using NLog;

namespace FirebirdTraceViewer.Services;

public class FileDialogService : IFileDialogService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IWindowProvider _windowProvider;

    public FileDialogService(IWindowProvider windowProvider)
    {
        _windowProvider = windowProvider
                          ?? throw new ArgumentNullException(nameof(windowProvider));
    }

    public async Task<IReadOnlyList<IStorageFile>> PickTraceFilesAsync()
    {
        var topLevel = _windowProvider.GetCurrent();

        if (topLevel == null)
        {
            Logger.Warn("Active window not found.");
            return [];
        }

        if (!topLevel.StorageProvider.CanOpen)
        {
            Logger.Warn("StorageProvider does not support opening files.");
            return [];
        }

        try
        {
            return await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select trace log files",
                    AllowMultiple = true,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Trace Logs")
                        {
                            Patterns = ["*.log", "*.txt"]
                        },
                        FilePickerFileTypes.All
                    ]
                });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error opening file selection dialog.");
            return [];
        }
    }
}