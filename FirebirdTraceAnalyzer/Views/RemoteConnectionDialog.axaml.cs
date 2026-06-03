using Avalonia.Controls;
using FirebirdTraceAnalyzer.ViewModels;

namespace FirebirdTraceAnalyzer.Views;

public partial class RemoteConnectionDialog : Window
{
    public RemoteConnectionDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Конструктор с ViewModel (для использования из кода)
    /// </summary>
    public RemoteConnectionDialog(RemoteConnectionDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Подписываемся на успешное подключение для автоматического закрытия окна
        viewModel.ConnectionEstablished += (_, eventArgs) => { Close(eventArgs != null); };
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        // Если ViewModel находится в процессе подключения, предупреждаем
        if (DataContext is RemoteConnectionDialogViewModel { IsConnecting: true })
        {
            // Можно добавить диалог подтверждения
        }
    }
}