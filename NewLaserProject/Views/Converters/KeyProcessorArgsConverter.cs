using NewLaserProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
//using System.Windows.Forms;

namespace NewLaserProject.Views.Converters
{
    
    internal class KeyProcessorArgsConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           if (value is KeyEventArgs args) 
           {
                try
                {
                    var isKeyDown = (Boolean)parameter;
                    return new KeyProcessorArgs(args, isKeyDown);
                }
                catch (Exception)
                {
                    return null;
                }
           }
           return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
