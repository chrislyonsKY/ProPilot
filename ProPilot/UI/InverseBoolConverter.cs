using System;
using System.Globalization;
using System.Windows.Data;

namespace ProPilot.UI;

/// <summary>
/// Inverts a boolean value. Used to disable input controls while processing.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
