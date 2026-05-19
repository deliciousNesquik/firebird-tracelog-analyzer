// ViewModels/FileCardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceViewer.Models;

namespace FirebirdTraceViewer.ViewModels;

public partial class FileCardViewModel : ViewModelBase
{
    // Используем Func<Task> вместо Action — позволяет правильно await'ить async операции
    private readonly Func<FileCardViewModel, Task> _onRemoveRequested;
    private readonly Func<FileCardViewModel, Task> _onParseRequested;

    [ObservableProperty]
    public partial TraceFileInfoModel FileInfo { get; set; }

    public FileCardViewModel(
        TraceFileInfoModel fileInfo,
        Func<FileCardViewModel, Task> onRemoveRequested,
        Func<FileCardViewModel, Task> onParseRequested)
    {
        FileInfo = fileInfo;
        _onRemoveRequested = onRemoveRequested ?? throw new ArgumentNullException(nameof(onRemoveRequested));
        _onParseRequested = onParseRequested ?? throw new ArgumentNullException(nameof(onParseRequested));
    }

    [RelayCommand]
    private Task RemoveFileAsync() => _onRemoveRequested(this);

    [RelayCommand]
    private Task ParseFileAsync() => _onParseRequested(this);
}