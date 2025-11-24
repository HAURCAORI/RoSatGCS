using FileListView.Interfaces;
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
using System.IO;
using System.Runtime.InteropServices;
using FileListView.ViewModels;
using RoSatGCS.Utils.Files;
using RoSatGCS.Utils.Logger;


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
            if (value is List<Models.SettingsModel.LanguageOption> temp)
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
            if (value is int v)
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
    public class FirstValidationErrorConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var hasError = values[0] as bool?;
            var errors = values[1] as ReadOnlyObservableCollection<ValidationError>;

            if (hasError == true && errors != null && errors.Count > 0)
                return errors[0].ErrorContent?.ToString();

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class FrequencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long v)
            {
                if (v >= 1000000000)
                {
                    return (v / 1000000000.0).ToString("0.000") + " GHz";
                }
                else if (v >= 1000000)
                {
                    return (v / 1000000.0).ToString("0.000") + " MHz";
                }
                else if (v >= 1000)
                {
                    return (v / 1000.0).ToString("0.000") + " KHz";
                }
                else
                {
                    return v.ToString() + " Hz";
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class InvisibleCharacterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s)
                return Binding.DoNothing;

            var builder = new StringBuilder();

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                switch (c)
                {
                    case ' ':
                        builder.Append('\u00B7'); // · for space
                        break;
                    case '\r':
                        if (i + 1 < s.Length && s[i + 1] == '\n')
                        {
                            builder.Append('\u21B5'); // ↵
                            builder.Append("\r\n");
                            i++; // skip \n
                        }
                        else
                        {
                            builder.Append('\u21B5'); // just in case \r appears alone
                            builder.Append("\r\n");
                        }
                        break;
                    default:
                        builder.Append(' '); // invisible zero-width space
                        break;
                }
            }

            return builder.ToString();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class StringToBytesCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return Encoding.UTF8.GetByteCount(str);
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    

    public class FileToByteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                if (FileSizeHelper.TryGetFileSize(path, out long size, out bool isDirectory))
                {
                    if (!isDirectory)
                    {
                        return FormatSize(size);
                    }
                }
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;

            if (bytes >= TB)
                return $"{(double)bytes / TB:0.#} TB";
            if (bytes >= GB)
                return $"{(double)bytes / GB:0.#} GB";
            if (bytes >= MB)
                return $"{(double)bytes / MB:0.#} MB";
            if (bytes >= KB * 50)
                return $"{(double)bytes / KB:0.#} KB";

            return $"{bytes} B";
        }
    }

    public class ListCreatedTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                return "Updated:" + dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ByteToModuleName : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) => value is byte b ? b switch
        {
            0x11 => "UHF",
            0x33 => "OBC",
            0x44 => "SBand",
            0x66 => "EPS BP",
            0x77 => "EPS PDM",
            _ => $"0x{b:X2}"
        } : "";
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    public class IndexInViewConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var item = values[0];
            var view = values[1] as ListCollectionView;
            if (item == null || view == null) return Binding.DoNothing;

            int idx = view.IndexOf(item);
            return (idx >= 0) ? (idx + 1).ToString() : "";
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class ByteListToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<byte> byteList)
            {
                return string.Join(" ", byteList.Select(b => b.ToString("X2")));
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public sealed class SeriesSelectorConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            if (value is PlotDataContainer ctn && parameter is string s &&
                ushort.TryParse(s, out var id))
                return ctn[id];
            return Binding.DoNothing;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    public sealed class PlotTitleConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            if (value is string[] strs && parameter is string s &&
                ushort.TryParse(s, out var id) && id < strs.Length)
                return strs[id];
            return Binding.DoNothing;
        }

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    public class BoolColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.Green : Brushes.Red;
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
    public class BoolShortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "T" : "F";
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class LogColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogEntry log)
            {
                return log.Level switch
                {
                    LogLevel.Info => Brushes.White,
                    LogLevel.Warning => new SolidColorBrush(Color.FromRgb(0xE6, 0xA2, 0x2C)),
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xF5, 0x6C, 0x6C)),
                    _ => Brushes.White
                };
            }
            return Binding.DoNothing;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ConOpsConverter :IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string[] modes = { "ROOT", "LEOP", "CMS", "NM", "NM_STATE_CHG", "NM_STATE_MS", "NM_STATE_PS", "EM" };
            string current_mode = "UNKNOWN";

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is bool b && b)
                {
                    current_mode = modes[i];
                    return current_mode;
                }
            }
            return current_mode;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
