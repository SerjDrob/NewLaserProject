using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using MachineControlsLibrary.Controls.GraphWin;
using NewLaserProject.ViewModels;

namespace NewLaserProject.Views.Converters
{
    internal class MarkPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var position = (TextPosition)value;
                return position;
            }
            catch (Exception)
            {
                return TextPosition.W;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
