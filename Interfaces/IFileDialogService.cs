using Avalonia.Platform.Storage;

namespace FTV.Interfaces;

public interface IFileDialogService
{
    Task<IReadOnlyList<IStorageFile>> OpenTraceFilesAsync();
}