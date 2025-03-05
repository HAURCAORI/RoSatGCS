using RoSatGCS.GCSTypes;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

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
            List<string> list = [];
            if (value is List<Models.WindowSettingsModel.LanguageOption> temp)
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

    public class ListBoxWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double listBoxWidth)
            {
                // Approximate scrollbar width (default is ~16px)
                double scrollBarWidth = SystemParameters.VerticalScrollBarWidth;

                // Ensure the width does not go negative
                return Math.Max(0, listBoxWidth - scrollBarWidth);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return enumValue.ToString();
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && targetType.IsEnum)
            {
                return Enum.Parse(targetType, strValue);
            }
            return Binding.DoNothing;
        }
    }

    public class EnumStructSizeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SatelliteFunctionTypeModel.ArgumentType enumValue)
            {
                if (enumValue == SatelliteFunctionTypeModel.ArgumentType.Struct)
                    return "Bytes";
                else if (enumValue == SatelliteFunctionTypeModel.ArgumentType.Enum)
                    return "Values";
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ByteStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is int v)
            {
                if (v > 1)
                    return v + " bytes";
                else
                    return v + " byte";
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value; // Return as is for safety
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }
    }

    public class ServiceStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ServiceManager.ServiceManager.ServiceState state)
            {
                switch (state)
                {
                    case ServiceManager.ServiceManager.ServiceState.Starting:
                        return IndicatorStatus.Neutral;
                    case ServiceManager.ServiceManager.ServiceState.Started:
                        return IndicatorStatus.Ok;
                    case ServiceManager.ServiceManager.ServiceState.Stopping:
                        return IndicatorStatus.Neutral;
                    case ServiceManager.ServiceManager.ServiceState.Stopped:
                        return IndicatorStatus.Error;
                    case ServiceManager.ServiceManager.ServiceState.Restart:
                        return IndicatorStatus.Neutral;
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

}

