using CommunityToolkit.Mvvm.Input;
using GMap.NET.MapProviders;
using Microsoft.Xaml.Behaviors;
using Newtonsoft.Json.Linq;
using NLog.Targets.Wrappers;
using RoSatGCS.Behaviors;
using RoSatGCS.Models;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static RoSatGCS.Models.SatelliteFunctionFileModel;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RoSatGCS.Controls
{
    /// <summary>
    /// ParameterTextBox.xaml에 대한 상호 작용 논리
    /// </summary>
    #region Regex
    public static class RegexHelper
    {
        private static Regex? _hexRegex;
        private static Regex? _numberRegex;

        public static Regex HexRegex => _hexRegex ??= new Regex("^[0-9A-Fa-f]$", RegexOptions.Compiled);
        public static Regex NumberRegex => _numberRegex ??= new Regex("^[0-9]$", RegexOptions.Compiled);
    }
    #endregion
    #region Types
    public enum BoxType
    {
        None, Boolean, Number, Enumeration, Array, Header
    }

    public enum InputMethod
    {
        STR, DEC, HEX, BIN, FLT
    }
    #endregion

    #region Behavior
    internal class InputTextBoxBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            "ValueModel",
            typeof(ParameterBoxValueModel),
            typeof(InputTextBoxBehavior),
            new PropertyMetadata(null));

        public ParameterBoxValueModel ValueModel
        {
            get => (ParameterBoxValueModel)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if(ValueModel.Parent != null)
                WeakEventManager<ParameterBox, EventArgs>.AddHandler(ValueModel.Parent, nameof(ParameterBox.CaretEvent), OnCaretToLast);
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            
        }

        protected override void OnDetaching()
        {
            if (ValueModel.Parent != null)
                WeakEventManager<ParameterBox, EventArgs>.RemoveHandler(ValueModel.Parent, nameof(ParameterBox.CaretEvent), OnCaretToLast);
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            base.OnDetaching();
        }

        private void OnCaretToLast(object? sender, EventArgs e)
        {
            AssociatedObject.CaretIndex = AssociatedObject.Text.Length;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox control) { return; }
            if (ValueModel == null) { return; }

            if (string.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;  // Ignore empty input
                return;
            }

            // Allowed common control keys (e.g., Backspace, Enter are handled separately in PreviewKeyDown)
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                return;
            }

            string inputChar = e.Text; // The actual character input by the user

            switch (ValueModel.Parent?.InputMethod) // .Parent.InputMethod
            {
                case InputMethod.BIN:
                    // Only UInt8 or Int8
                    if (!(inputChar == "0" || inputChar == "1") || ValueModel.Value.Length >= 8)
                        e.Handled = true;
                    break;

                case InputMethod.HEX:
                    // Allow 0-9 and A-F
                    if (ValueModel.Value.Replace(" ", "").Length >= 2 * ValueModel.ByteSize || !RegexHelper.HexRegex.IsMatch(inputChar))
                    {
                        e.Handled = true;
                        return;
                    }

                    // String Formatter
                    string newText = ValueModel.Value.Insert(control.CaretIndex, e.Text).Replace(" ","");

                    // Format the text with spaces every 2 characters
                    StringBuilder formattedText = new();
                    for (int i = 0; i < newText.Length; i++)
                    {
                        if (i > 0 && i % (ValueModel.TypeSize * 2) == 0)
                            formattedText.Append(' ');
                        formattedText.Append(newText[i]);
                    }

                    int caret = control.CaretIndex + 1;

                    // Update the TextBox
                    ValueModel.Value = formattedText.ToString();

                    // String Boundary Handler
                    if (caret > 0 && ValueModel.Value[caret-1] == ' ')
                        caret++;

                    control.CaretIndex = caret;
                    e.Handled = true;

                    break;

                case InputMethod.DEC:
                    // Allow only digits, and '-' at the beginning for signed numbers
                    DataType p = ValueModel.DataType;

                    if ( (p == DataType.Int8 || p == DataType.Int16 || p == DataType.Int32 || p == DataType.Int64 || p == DataType.Integer)
                        && inputChar == "-" && ValueModel.Value.Length == 0) { }
                    else if (!RegexHelper.NumberRegex.IsMatch(inputChar))
                        e.Handled = true;
                    break;

                case InputMethod.FLT:
                    if (inputChar == "+" || inputChar == "-")
                    {
                        // Allow '-' only at the start
                        if (ValueModel.Value.Length == 0) { }
                        // Allow +/- only after 'e' or 'E'
                        else if (!(ValueModel.Value.EndsWith("e", StringComparison.OrdinalIgnoreCase)))
                            e.Handled = true;
                    }
                    else if (inputChar == ".")
                    {
                        // Allow only one decimal point
                        if (ValueModel.Value.Contains('.')) e.Handled = true;
                    }
                    else if (inputChar.Equals("e", StringComparison.OrdinalIgnoreCase))
                    {
                        // Allow only one 'e' or 'E' and it must not be the first character
                        if (ValueModel.Value.Contains('e') || ValueModel.Value.Contains('E') || ValueModel.Value.Length == 0)
                            e.Handled = true;
                    }
                    else if (!RegexHelper.NumberRegex.IsMatch(inputChar))
                    {
                        e.Handled = true;
                    }
                    break;

                case InputMethod.STR:
                    if (ValueModel.Value.Length + 1 >= ValueModel.ByteSize)
                    {
                        e.Handled = true;
                        return;
                    }
                    break;

                default:
                    e.Handled = true;
                    break;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            if (ValueModel.IsReadOnly)
            {
                e.Handled = true;
            }
            else if (ValueModel.DataType == DataType.String)
            {

            }
            else if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = false; // Allow editing
            }
            else if (e.Key == Key.Space)
                e.Handled = true; // Disable space
            else if (e.Key == Key.Tab && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                // SwapInputMode
                if (ValueModel != null)
                {
                    ValueModel.Parent?.SwapInputMode();

                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Tab)
                OnCaretToLast(null, new EventArgs());
        }
    }
    #endregion

    #region Converters
    public class ValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReadOnlyObservableCollection<ValidationError> e)
            {
                if(e.Count > 0)
                return e[0].ErrorContent;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
    public class InputMethodStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is InputMethod v)
            {
                return v.ToString();
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
    public class InputMethodPrefixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InputMethod v)
            {
                if (v == InputMethod.HEX)
                    return "0x";
                if (v == InputMethod.BIN)
                    return "0b";
                else
                    return "";
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string v)
            {
                if (v == "true")
                    return true;
                else if (v == "false")
                    return false;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool v)
            {
                if (v == true)
                    return "true";
                else
                    return "false";
            }
            return Binding.DoNothing;
        }
    }
    #endregion

    #region TemplateSelector
    public class ParameterBoxTemplateSelector : DataTemplateSelector
    {
        public ParameterBoxTemplateSelector() { }

        public DataTemplate? BooleanTemplate { get; set; }
        public DataTemplate? NumberTemplate { get; set; }
        public DataTemplate? EnumerationTemplate { get; set; }
        public DataTemplate? ArrayTemplate { get; set; }
        public DataTemplate? NoneTemplate { get; set; }
        public DataTemplate? HeaderTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {

            if (item != null && item is ParameterBoxValueModel el)
            {
                if (el.Parent?.BoxType == BoxType.Boolean)
                    return BooleanTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent?.BoxType == BoxType.Number)
                    return NumberTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent?.BoxType == BoxType.Enumeration)
                    return EnumerationTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent?.BoxType == BoxType.Array)
                    return ArrayTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent?.BoxType == BoxType.None)
                    return NoneTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent?.BoxType == BoxType.Header)
                    return HeaderTemplate ?? base.SelectTemplate(item, container);
            }

            return base.SelectTemplate(item, container);
        }
    }
    #endregion

    #region Helper
    public class Helper
    {
        static bool TryHexToFloat(string hex, out float value, bool inputIsBigEndian = true)
        {
            value = 0f;
            if (hex == null) return false;

            // Normalize (allow "0x" and underscores)
            hex = hex.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
            hex = hex.Replace("_", "");

            if (hex.Length != 8) return false; // float = 4 bytes = 8 hex chars
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bits))
                return false;

            // If the hex string is big-endian (network order) and the machine is little-endian (x86/x64),
            // reverse to get the platform byte order. Flip this condition if your stored hex is little-endian.
            if (BitConverter.IsLittleEndian == inputIsBigEndian)
                bits = BinaryPrimitives.ReverseEndianness(bits);

            value = BitConverter.UInt32BitsToSingle(bits);

            return true;
        }

        static bool TryHexToDouble(string hex, out double value, bool inputIsBigEndian = true)
        {
            value = 0d;
            if (hex == null) return false;

            // Normalize (allow "0x" and underscores)
            hex = hex.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
            hex = hex.Replace("_", "");

            if (hex.Length != 16) return false; // double = 8 bytes = 16 hex chars
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bits))
                return false;

            // Adjust for endian mismatch
            if (BitConverter.IsLittleEndian == inputIsBigEndian)
                bits = BinaryPrimitives.ReverseEndianness(bits);

            value = BitConverter.UInt64BitsToDouble(bits);

            return true;
        }


        public static int GetBaseFromInputMode(InputMethod? mode)
        {
            var currentBase = 10;
            if (mode == null)
                return currentBase;
            else if (mode == InputMethod.BIN)
                currentBase = 2;
            else if (mode == InputMethod.HEX)
                currentBase = 16;
            return currentBase;
        }
        public static string? BaseChange(string str, DataType type, int prevBase, int currentBase)
        {
            string? ret = null;
            switch (type)
            {
                case DataType.UInt8:
                    ret = Convert.ToString(Convert.ToByte(str, prevBase), currentBase);
                    break;
                case DataType.Int8:
                    ret = Convert.ToString(Convert.ToSByte(str, prevBase), currentBase);
                    if (currentBase == 16)
                        ret = ret.PadLeft(2, '0')[Math.Max(0, ret.Length - 2)..];
                    else if (currentBase == 2)
                        ret = ret.PadLeft(8, '0')[Math.Max(0, ret.Length - 8)..];
                    break;
                case DataType.UInt16:
                    ret = Convert.ToString(Convert.ToUInt16(str, prevBase), currentBase);
                    break;
                case DataType.Int16:
                    ret = Convert.ToString(Convert.ToInt16(str, prevBase), currentBase);
                    break;
                case DataType.UInt32:
                    ret = Convert.ToString(Convert.ToUInt32(str, prevBase), currentBase);
                    break;
                case DataType.Integer:
                case DataType.Int32:
                    ret = Convert.ToString(Convert.ToInt32(str, prevBase), currentBase);
                    break;
                case DataType.UInt64:
                    if (prevBase == 10 && currentBase == 16)
                        ret = string.Format("{0:X}", Convert.ToUInt64(str, prevBase));
                    else if (prevBase == 16 && currentBase == 10)
                        ret = Convert.ToUInt64(str, 16).ToString();
                    break;
                case DataType.Int64:
                    ret = Convert.ToString(Convert.ToInt64(str, prevBase), currentBase);
                    break;
                case DataType.Boolean:
                    ret = str;
                    return ret;
                case DataType.String:
                    ret = str;
                    return ret;
                case DataType.Float:
                    if (!TryHexToFloat(str, out var f, inputIsBigEndian: false))
                        throw new FormatException($"Invalid float hex: {str}");

                    ret = f.ToString("R", CultureInfo.InvariantCulture);
                    return ret;
                case DataType.Double:
                    if (!TryHexToDouble(str, out var d, inputIsBigEndian: false))
                        throw new FormatException($"Invalid double hex: {str}");

                    ret = d.ToString("R", CultureInfo.InvariantCulture);
                    return ret;
                default:
                    break;
            }
            if (ret != null && currentBase == 16 && ret.Length % 2 == 1)
                ret = "0" + ret;
            return ret;
        }

        public static int GetTypeSize(object o)
        {
            if (o is byte || o is sbyte)
                return 1;
            if (o is ushort || o is short)
                return 2;
            if (o is uint || o is int)
                return 4;
            if (o is ulong || o is long)
                return 8;
            if (o is float)
                return 4;
            if (o is double)
                return 8;
            if (o is string str)
                return str.Length;
            return 0;
        }
    }
    #endregion

    public partial class ParameterBox : UserControl, INotifyPropertyChanged
    {
        #region Notifier
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        #region Dependency Property
        public static readonly DependencyProperty ParameterProperty =
        DependencyProperty.Register(
           "Parameter",
           typeof(ParameterModel),
           typeof(ParameterBox),
           new PropertyMetadata(OnParameterChanged));
        #endregion

        public static event EventHandler? CaretEvent;

        private static void SetEnumerationParameterHelper(ParameterBox param)
        {
            if (param.Parameter.CommandModel == null)
            {
                param.Error = "Null Command Model";
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, 0, 1, param.Parameter.IsReadOnly);
                param.ValueModels.Add(v);
                return;
            }
            if (param.Parameter.ByteSize % 1 != 0 || (!param.Parameter.IsArray && param.Parameter.ByteSize != 1))
            {
                param.Error = "Invalid Enumeration Size";
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, 1, 1, param.Parameter.IsReadOnly);
                param.ValueModels.Add(v);
                return;
            }
            param.Length = param.Parameter.IsArray ? param.Parameter.ByteSize : 1;

            param.BoxType = BoxType.Enumeration;

            var id = 0;
            for (int i = 0; i < param.Length; i++)
            {
                // Create Enumeration
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, 1, 1, param.Parameter.IsReadOnly, param.Length > 1 ? id++ : -1);

                var str = param.Parameter.DataTypeString;
                if (str.EndsWith("[]"))
                    str = str[..^3];

                param.Parameter.CommandModel.AssociatedType.TryGetValue(str, out SatelliteFunctionTypeModel? tmp);

                if (tmp == null || tmp.Type != SatelliteFunctionTypeModel.ArgumentType.Enum)
                { param.Error = "Unknown Enumeration Value"; break; }
                SatelliteFunctionTypeModel type = (SatelliteFunctionTypeModel) tmp.Clone();

                v.EnumerationValues = type.Parameters;
                v.SelectedEnumItem = v.EnumerationValues.FirstOrDefault();

                param.ValueModels.Add(v);
            }
        }

        private static void SetNumberParameterHelper(ParameterBox param, int typeSize, bool isfloat = false, bool includeBinary = false)
        {
            if(param.Parameter.ByteSize % typeSize != 0 || (!param.Parameter.IsArray && param.Parameter.ByteSize != typeSize))
            {
                param.Error = "Invalid Size";
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, typeSize, 1, param.Parameter.IsReadOnly);
                param.ValueModels.Add(v);
                return;
            }

            param.Length = param.Parameter.IsArray ? param.Parameter.ByteSize / typeSize : 1;

            if (isfloat)
            {
                param.AvailableInput.Add(InputMethod.FLT);
            }
            else
            {
                if ((!param.IsArray || param.ArrayExpand))
                    param.AvailableInput.Add(InputMethod.DEC);
                param.AvailableInput.Add(InputMethod.HEX);
                if (includeBinary && (!param.IsArray || param.ArrayExpand))
                    param.AvailableInput.Add(InputMethod.BIN);
            }

            param.BoxType = BoxType.Number;

            if (param.ArrayExpand)
            {
                var id = 0;
                for (int i = 0; i < param.Length; i++)
                {
                    var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, typeSize, 1, param.Parameter.IsReadOnly, param.Length > 1 ? id++ : -1);
                    param.ValueModels.Add(v);
                }   
            }
            else
            {
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, param.IsArray, typeSize, param.Length, param.Parameter.IsReadOnly);
                param.ValueModels.Add(v);
            }
        }

        private static void InitializeParameter(ParameterBox param, bool swapExpand = false)
        {
            List<string> prevValues = [];

            if (swapExpand)
            {
                foreach (var i in param.ValueModels)
                {
                    if (i.HasErrors && !param.ArrayExpand) { continue; }
                    if (i.Value == null || i.Value == string.Empty) { continue; }
                    if (param.InputMethod == InputMethod.FLT || param.InputMethod == InputMethod.STR) { continue; }

                    int format = (i.TypeSize * 2);

                    if (param.InputMethod == InputMethod.HEX)
                    {
                        string tmp = i.Value.Replace(" ", "");
                        if (tmp.Length % 2 != 0)
                            continue;

                        tmp = tmp.PadLeft(format, '0');

                        List<string> hexList = Enumerable.Range(0, tmp.Length / format).Select(i => tmp.Substring(i * format, format)).ToList();
                        prevValues.AddRange(hexList);
                        continue;
                    }

                    var currentBase = 10;
                    if (param.InputMethod == InputMethod.BIN)
                        currentBase = 2;

                    string? ret = Helper.BaseChange(i.Value, i.DataType, currentBase, 16);

                    if (ret != null)
                    {
                        ret = ret.PadLeft(format, '0');
                        prevValues.Add(ret);
                    }
                }
            }


            param.ValueModels.Clear();
            param.BoxType = BoxType.None;

            param.inputMethodIndex = 0;
            param.AvailableInput.Clear();

            if (param.Parameter.CommandModel == null)
            {
                param.Error = "Null Command Model";
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, 0, 1, param.Parameter.IsReadOnly);
                param.ValueModels.Add(v);
                return;
            }

            switch (param.Parameter.DataType)
            {
                case DataType.Int8:
                case DataType.UInt8:
                    SetNumberParameterHelper(param, 1, false, true);
                    break;
                case DataType.Int16:
                case DataType.UInt16:
                    SetNumberParameterHelper(param, 2);
                    break;
                case DataType.Integer:
                case DataType.Int32:
                case DataType.UInt32:
                    SetNumberParameterHelper(param, 4);
                    break;
                case DataType.Int64:
                case DataType.UInt64:
                    SetNumberParameterHelper(param, 8);
                    break;
                case DataType.Float:
                    SetNumberParameterHelper(param, 4, true);
                    break;
                case DataType.Double:
                    SetNumberParameterHelper(param, 8, true);
                    break;
                case DataType.String:
                    if (param.Parameter.IsArray == true)
                    { param.Error = "Invalid String Array"; break; }
                    param.Length = 1;
                    param.AvailableInput.Add(InputMethod.STR);

                    param.BoxType = BoxType.Array;

                    var s = new ParameterBoxValueModel(param, param.Parameter.DataType, false, param.Parameter.ByteSize, 1, param.Parameter.IsReadOnly);
                    param.ValueModels.Add(s);
                    break;
                case DataType.Boolean:
                    if (param.Parameter.IsArray == true)
                    { param.Error = "Invalid Boolean Array"; break; }
                    param.Length = 1;

                    param.BoxType = BoxType.Boolean;

                    var b = new ParameterBoxValueModel(param, param.Parameter.DataType, false, 1, 1, param.Parameter.IsReadOnly)
                    {
                        Value = "false"
                    };
                    param.ValueModels.Add(b);
                    break;
                case DataType.Enumeration:
                    if (param.Parameter.BaseType == SatelliteFunctionTypeModel.ArgumentType.Enum)
                        SetEnumerationParameterHelper(param);
                    else
                        param.Error = "Invalid Data Type";
                    break;
                case DataType.ByteBuffer:
                    if (param.Parameter.IsArray == true)
                    { param.Error = "Invalid ByteBuffer Array"; break; }

                    param.AvailableInput.Add(InputMethod.HEX);

                    param.BoxType = BoxType.Number;

                    var bb = new ParameterBoxValueModel(param, param.Parameter.DataType, false, param.Parameter.ByteSize, 1, param.Parameter.IsReadOnly);
                    param.ValueModels.Add(bb);
                    break;
                case DataType.None:
                    if (param.Parameter.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct)
                    {
                        param.IsHeader = true;

                        param.BoxType = BoxType.Header;

                        var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, param.Parameter.ByteSize, 1, param.Parameter.IsReadOnly);
                        param.ValueModels.Add(v);
                    }
                    else
                        param.Error = "Invalid Struct Header";
                    break;
                default:
                    { param.Error = "Invalid Data Type"; break; }
            }

            if(param.Error != string.Empty)
            {
                var v = new ParameterBoxValueModel(param, param.Parameter.DataType, false, 0, 1, param.Parameter.IsReadOnly);
                param.ValueModels.Add(v);
                return;
            }

            if(swapExpand)
            {
                if(param.ArrayExpand)
                {
                    var currentBase = 10;
                    if (param.InputMethod == InputMethod.BIN)
                        currentBase = 2;
                    else if (param.InputMethod == InputMethod.HEX)
                        currentBase = 16;

                    for (int i = 0; i < Math.Min(prevValues.Count, param.ValueModels.Count); i++) 
                    {
                        var Value = prevValues[i];

                        string? ret = Helper.BaseChange(Value, param.Parameter.DataType, 16, currentBase);

                        if(ret != null)
                            param.ValueModels[i].Value = ret;
                    }
                }
                else
                {
                    var f = param.ValueModels.FirstOrDefault();
                    if(f!= null && param.InputMethod == InputMethod.HEX)
                    {
                        f.Value = string.Join(" ", prevValues);
                    }
                }
            }
        }

        private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ParameterBox param)
            {
                param.Parameter.PullValue = new RelayCommand(param.OnPullValue);
                InitializeParameter(param);
                param.OnPushValue();

                param.Parameter.Received += param.OnValueChanged;
                
                param.Parameter.Init = true;
            }
        }
        
        private void OnPullValue()
        {
            if(Parameter.Value == null) { return; }

            Parameter.Value.Clear();
            foreach (var v in ValueModels)
            {
                try
                {
                    var ret = v.Result;
                    if (ret != null)
                        Parameter.Value.AddRange(ret);
                }
                catch{
                    continue;
                }
            }
        }

        private void OnPushValue()
        {
            if (Parameter.Value == null) { return; }

            int index = 0;
            int total = Parameter.Value.Sum(o => Helper.GetTypeSize(o));
            

            foreach (var v in ValueModels)
            {
                var size = 0;
                var bs = v.ByteSize;
                List<object> value = [];

                for (; index < Parameter.Value.Count; index++)
                {
                    var o = Parameter.Value[index];
                    value.Add(o);

                    size += Helper.GetTypeSize(o);
                    if (size == bs)
                    {
                        index++;
                        break;
                    }
                }

                v.Result = value;
            }
        }

        private void OnValueChanged(object? sender, EventArgs e)
        {
            OnPushValue();
        }


        public ParameterModel Parameter
        {
            get => (ParameterModel)GetValue(ParameterProperty);
            set => SetValue(ParameterProperty, value);
        }


        private ObservableCollection<ParameterBoxValueModel> _valueModels = [];
        private List<InputMethod> _availableInput = [];
        private BoxType _boxType = BoxType.Number;

        private string _error = "";
        private int _length = 0;
        private int inputMethodIndex = 0;
        private bool _arrayExpand = false;
        private bool _isheader = false;

        public ObservableCollection<ParameterBoxValueModel> ValueModels
        {
            get => _valueModels;
            set { _valueModels = value;
                OnPropertyChanged(); }
        }

        public List<InputMethod> AvailableInput
        {
            get => _availableInput;
            set => _availableInput = value;
        }

        public InputMethod InputMethod
        {
            get => (AvailableInput.Count > inputMethodIndex) ? AvailableInput[inputMethodIndex] : InputMethod.STR;
        }

        public BoxType BoxType { get => _boxType; set => _boxType = value; }
        public int Length { get => _length; set => _length = value; }
        public string Error { get => _error; set => _error = value; }
        public bool ArrayExpand { get => _arrayExpand; set => _arrayExpand = value; }
        public bool IsHeader { get => _isheader; set => _isheader = value; }

        public string ParameterName
        {
            get => ((Parameter.Sequence != string.Empty) ? (Parameter.Sequence + ") ") : "" )+ Parameter?.Name ?? "-";
            //((Parameter.Sequence >= 0) ? ( Parameter.Sequence) : ("")) + ((IsHeader) ? "" : ("." + Parameter.Index)) + ") " 
        }

        public string Description { get => Parameter?.Description ?? "-"; }

        public bool IsArray { get => Parameter?.IsArray ?? false; }

        public bool CanExpand
        {
            get => (Parameter?.BaseType != SatelliteFunctionTypeModel.ArgumentType.Enum) && (Parameter?.IsArray ?? false) && Parameter.ByteSize < 16 ;
        }

        public ParameterBox()
        {
            InitializeComponent();
        }

        public void SwapInputMode()
        {
            var prev = InputMethod;
            inputMethodIndex = (inputMethodIndex + 1) % AvailableInput.Count;
            var current = InputMethod;
            OnPropertyChanged(nameof(InputMethod));
            foreach (var v in _valueModels)
            {
                v.SwapRefresh(prev, current);
            }

            CaretEvent?.Invoke(this, new EventArgs());
        }
        private void SwapInputMode(object sender, MouseButtonEventArgs? e)
        {
            SwapInputMode();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {

            InitializeParameter(this, true);
            OnPushValue();
        }
    }
}
