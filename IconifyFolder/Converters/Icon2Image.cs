using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace IconifyFolder.Converters
{
    public class Icon2Image : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Icon icon)
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
               icon.Handle,
               new Int32Rect(0, 0, icon.Width, icon.Height),
               BitmapSizeOptions.FromEmptyOptions());
            }

            return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}