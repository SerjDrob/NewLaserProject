﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using NewLaserProject.Data.Models;

namespace NewLaserProject.Views.Converters
{
    internal class ProcTimeToProcEndConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var procTimeLog = (ProcTimeLog)value;
                if (procTimeLog.Success)
                {
                    return "Завершён";
                }
                else
                {
                    if (procTimeLog.ExceptionMessage!=null)
                    {
                        return "Ошибка";
                    }
                    else
                    {
                        return "Отменён";
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
