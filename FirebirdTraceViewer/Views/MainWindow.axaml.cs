using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FirebirdTraceViewer.Services;
using FirebirdTraceViewer.ViewModels;

namespace FirebirdTraceViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}