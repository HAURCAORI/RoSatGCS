using RoSatGCS.Utils.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace RoSatGCS.Utils.Converter
{
    public class BaseConverter
    {
        public static T ConvertEnum<T>(object o)
        {
            T enumVal = (T)Enum.ToObject(typeof(T), o);
            return enumVal;
        }
    }

    public class LanguageConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = value as List<Models.WindowSettingsModel.LanguageOption>;
            List<string> list = new List<string>();
            if (temp != null)
            {
                foreach (var item in temp)
                {
                    var str = TranslationSource.Instance[item.ToString()];
                    if (str != null)
                    {
                        list.Add(str);
                    }
                }
                
            }
            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
