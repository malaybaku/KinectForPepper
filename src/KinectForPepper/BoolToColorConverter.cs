using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Baku.KinectForPepper
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool b = (bool)value;
                return b ? Brushes.Green : Brushes.Red;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //特に何も起きない
            return value;
        }
    }
}
