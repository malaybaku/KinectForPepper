using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Baku.KinectForPepper
{
    public class BoolToConnectionStatusStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool b = (bool)value;
                return b ? "Connected" : "Disconnected";
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //特に何も起きない
            return DependencyProperty.UnsetValue;
        }
    }
}
