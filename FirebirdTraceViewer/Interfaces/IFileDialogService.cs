using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FirebirdTraceViewer.Interfaces;

public interface IFileDialogService
{
    Task<IReadOnlyList<IStorageFile>> PickTraceFilesAsync();
}