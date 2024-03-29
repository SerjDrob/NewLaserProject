﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using NewLaserProject.Data.Models;

namespace NewLaserProject.Views.Converters
{
    internal class ProcTimeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var procTimeLog = (ProcTimeLog)value;
                if (procTimeLog.Success)
                {
                    return Brushes.YellowGreen;
                }
                else
                {
                    if (procTimeLog.ExceptionMessage != null)
                    {
                        return Brushes.Red;
                    }
                    else
                    {
                        return Brushes.Orange;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
