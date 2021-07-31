using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Diagnostics;

namespace ClipIt
{
    [ValueConversion(typeof(Boolean), typeof(BitmapSource))]
    public class IsPlayingImageSrcConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isPlaying = (bool) value;

            // Create image source
            BitmapImage bi = new BitmapImage();

            // BitmapImage.UriSource must be in a BeginInit/EndInit block
            bi.BeginInit();
            if (isPlaying) {
                bi.UriSource = new Uri("pack://application:,,,/Images/pause_media.png", UriKind.RelativeOrAbsolute);
            } else {
                bi.UriSource = new Uri("pack://application:,,,/Images/play_media.png", UriKind.RelativeOrAbsolute);
            }
            
            bi.EndInit();

            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
