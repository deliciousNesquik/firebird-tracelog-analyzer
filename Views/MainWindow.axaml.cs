using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FTV.Services;
using FTV.ViewModels;

namespace FTV.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        var fileDialogService = new FileDialogService(this);

        DataContext = new MainWindowViewModel(fileDialogService);
    }
}