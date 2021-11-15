using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace LiveHome.Client.Mobile
{
    public class BoolToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            switch (value)
            {
                case bool val:
                    if (val)
                    {
                        return "是";
                    }
                    else
                    {
                        return "否";
                    }
                default:
                    return "?";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }
    }
}
