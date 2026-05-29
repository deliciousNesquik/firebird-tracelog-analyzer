using Avalonia.Controls;

namespace FirebirdTraceViewer.Interfaces;

public interface IWindowProvider
{
    TopLevel? GetCurrent();
}