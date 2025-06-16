using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Hourglass.Windows;

public sealed class VisibilityToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}