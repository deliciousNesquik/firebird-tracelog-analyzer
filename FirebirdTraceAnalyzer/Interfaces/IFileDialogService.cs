using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FirebirdTraceAnalyzer.Interfaces;

public interface IFileDialogService
{
    Task<IReadOnlyList<IStorageFile>> PickTraceFilesAsync();
}