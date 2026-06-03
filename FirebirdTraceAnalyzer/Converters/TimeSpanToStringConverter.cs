using System.Globalization;
using Avalonia.Data.Converters;

namespace FirebirdTraceAnalyzer.Converters;

/// <summary>
/// Конвертирует TimeSpan в читаемую строку (mm:ss или hh:mm:ss)
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            // Если время неизвестно или отрицательное
            if (timeSpan <= TimeSpan.Zero)
            {
                return "--:--";
            }

            // Если больше часа - показываем часы
            if (timeSpan.TotalHours >= 1)
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }

            // Иначе только минуты и секунды
            return timeSpan.ToString(@"mm\:ss");
        }

        return "--:--";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}