using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FirebirdTraceAnalyzer.Converters;

/// <summary>
/// Конвертирует скорость в байтах/сек в читаемый формат
/// </summary>
public class SpeedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double speed)
        {
            return FormatSpeed(speed);
        }

        return "0 B/s";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:F0} B/s";
        
        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:F2} KB/s";
        
        return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
    }
}