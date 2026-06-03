using Avalonia.Controls;
using Avalonia.Interactivity;
using FirebirdTraceAnalyzer.ViewModels;

namespace FirebirdTraceAnalyzer.Views;

public partial class DownloadProgressWindow : Window
{
    public DownloadProgressWindow()
    {
        InitializeComponent();
    }

    public DownloadProgressWindow(DownloadProgressViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Запрещаем закрытие во время загрузки
        Closing += (_, e) =>
        {
            if (DataContext is DownloadProgressViewModel vm && vm.IsDownloading)
            {
                e.Cancel = true; // Запрещаем закрытие
                
                // Можно показать диалог подтверждения
                // "Вы уверены, что хотите отменить загрузку?"
            }
        };

        // Автоматически закрываем после завершения (опционально)
        viewModel.Completed += (_, _) =>
        {
            // Можно автоматически закрыть или оставить открытым
            // Close();
        };
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}