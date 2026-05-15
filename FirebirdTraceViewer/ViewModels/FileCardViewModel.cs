using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceViewer.Models;

namespace FirebirdTraceViewer.ViewModels;

public partial class FileCardViewModel : ViewModelBase
{
    private readonly Action<FileCardViewModel> _onRemoveRequested;

    [ObservableProperty] public partial TraceFileInfoModel FileInfo { get; set; }

    public FileCardViewModel(TraceFileInfoModel fileInfo, Action<FileCardViewModel> onRemoveRequested)
    {
        FileInfo = fileInfo;
        _onRemoveRequested = onRemoveRequested;
    }
    
    [RelayCommand]
    private async Task RemoveFileAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { _onRemoveRequested?.Invoke(this); });
    }
}
