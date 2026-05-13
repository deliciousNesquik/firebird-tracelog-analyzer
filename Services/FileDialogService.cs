using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FTV.Interfaces;

namespace FTV.Services;

public class FileDialogService : IFileDialogService
{
    private readonly Window _window;

    public FileDialogService(Window window)
    {
        _window = window;
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenTraceFilesAsync()
    {
        return await _window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Выбор файлов логов",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    FilePickerFileTypes.All
                ]
            });
    }
}