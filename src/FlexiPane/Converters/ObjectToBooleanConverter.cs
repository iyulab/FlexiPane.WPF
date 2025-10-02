using System;
using System.Globalization;
using System.Windows.Data;

namespace FlexiPane.Converters;

/// <summary>
/// Converts an object to boolean - returns true if object is not null, false otherwise
/// </summary>
public class ObjectToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML
    /// </summary>
    public static readonly ObjectToBooleanConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ObjectToBooleanConverter does not support ConvertBack");
    }
}