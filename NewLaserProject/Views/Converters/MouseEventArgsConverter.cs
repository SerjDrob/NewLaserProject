using NewLaserProject.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NewLaserProject.Views.Converters
{
    public class MouseEventArgsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var mouseEvent = value as MouseButtonEventArgs;
            var device = mouseEvent?.Device as MouseDevice;
            var control = mouseEvent?.Source as System.Windows.Controls.Image;
            if (device is not null && control is not null)
            {
                var width = (double)control.ActualWidth;
                var height = (double)control.ActualHeight;
                var imgX = (device.GetPosition(control).X - (width / 2)) / width;
                var imgY = (device.GetPosition(control).Y - (height / 2)) / height;
                return (imgX,imgY); 
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
