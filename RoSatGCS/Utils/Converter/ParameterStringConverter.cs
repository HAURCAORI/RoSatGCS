using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using RoSatGCS.Controls;
using RoSatGCS.Models;
using static RoSatGCS.Models.SatelliteFunctionFileModel;

namespace RoSatGCS.Utils.Converter
{
    public class ParameterStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ParameterModel p)
            {
                if(p.Value == null) { return "null"; }
                if(p.Value.Count == 0) { return "empty"; }

                switch (p.DataType)
                {
                    case DataType.UInt8:
                        if(p.IsArray)
                            return string.Join("", p.Value.OfType<byte>().Select(b => b.ToString("X2")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Int8:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<sbyte>().Select(b => b.ToString("X2")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.UInt16:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<ushort>().Select(b => b.ToString("X4")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Int16:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<short>().Select(b => b.ToString("X4")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.UInt32:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<uint>().Select(b => b.ToString("X8")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Integer:
                    case DataType.Int32:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<int>().Select(b => b.ToString("X8")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.UInt64:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<ulong>().Select(b => b.ToString("X16")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Int64:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<long>().Select(b => b.ToString("X16")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Float:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<float>().Select(b => b.ToString("X8")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Double:
                        if (p.IsArray)
                            return string.Join("", p.Value.OfType<double>().Select(b => b.ToString("X16")));
                        return p.Value[0].ToString() ?? "none";
                    case DataType.String:
                        return p.Value[0].ToString() ?? "none";
                    case DataType.Boolean:
                        if (p.IsArray)
                            return "Booleans";
                        return (bool)p.Value[0] ? "True" : "False";
                    case DataType.Enumeration:
                        if(p.CommandModel == null) { return "Enum Null"; }
                        if (p.CommandModel.AssociatedType.TryGetValue(p.UserDefinedType, out SatelliteFunctionTypeModel? tmp))
                        {
                            var r = tmp.Parameters.FirstOrDefault(o => o.Id == (byte) p.Value[0]);
                            if (r != null)
                                return r.Name;
                            else
                                return "Invalid Enum";
                        }
                        return "Invalid Enum";
                    case DataType.ByteBuffer:
                        return string.Join("", p.Value.OfType<byte>().Select(b => b.ToString("X2")));
                    case DataType.UserDefined:
                        return "UserDefined";
                }
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ParameterTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = "";
            if (value is ParameterModel p)
            {
                switch (p.DataType)
                {
                    case DataType.UInt8:
                        str = "UInt8";
                        break;
                    case DataType.Int8:
                        str = "Int8";
                        break;
                    case DataType.UInt16:
                        str = "UInt16";
                        break;
                    case DataType.Int16:
                        str = "Int16";
                        break;
                    case DataType.UInt32:
                        str = "UInt32";
                        break;
                    case DataType.Integer:
                        str = "Integer";
                        break;
                    case DataType.Int32:
                        str = "Int32";
                        break;
                    case DataType.UInt64:
                        str = "UInt64";
                        break;
                    case DataType.Int64:
                        str = "Int64";
                        break;
                    case DataType.Float:
                        str = "Float";
                        break;
                    case DataType.Double:
                        str = "Double";
                        break;
                    case DataType.String:
                        str = "String";
                        break;
                    case DataType.Boolean:
                        str = "Boolean";
                        break;
                    case DataType.Enumeration:
                        str = "Enum";
                        break;
                    case DataType.ByteBuffer:
                        str = "ByteBuffer";
                        break;
                    case DataType.UserDefined:
                        str = "UserDefined";
                        break;
                }
                if (p.IsArray)
                {
                    str += "[]";
                }
                str = "[" + str + "]";
            }
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
