using Avalonia.Controls;
using Avalonia.Interactivity;
using FirebirdTraceAnalyzer.Models;
using FirebirdTraceAnalyzer.ViewModels;

namespace FirebirdTraceAnalyzer.Views;

public partial class RemoteFileSelectionDialog : Window
{
    public RemoteFileSelectionDialog()
    {
        InitializeComponent();
    }

    public RemoteFileSelectionDialog(RemoteFileSelectionViewModel viewModel) : this()
    {
        /*DataContext = viewModel;
        
        // Подписываемся на выбор файлов
        viewModel.FilesSelected += (_, files) =>
        {
            Close(files); // Возвращаем выбранные файлы
        };*/
        
        DataContext = viewModel;
    
        EventHandler<IReadOnlyList<RemoteFileInfo>>? handler = null;
        handler = (_, files) =>
        {
            viewModel.FilesSelected -= handler; // Отписка
            Close(files);
        };
    
        viewModel.FilesSelected += handler;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null); // null = отмена
    }
}