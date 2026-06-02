using Avalonia.Controls;

namespace FirebirdTraceAnalyzer.Interfaces;

public interface IWindowProvider
{
    TopLevel? GetCurrent();
}