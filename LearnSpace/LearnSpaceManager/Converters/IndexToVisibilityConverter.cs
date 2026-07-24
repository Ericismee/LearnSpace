using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LearnSpaceManager.Converters
{
    public class IndexToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selectedIndex && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetIndex))
                {
                    return selectedIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
