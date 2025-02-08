using CommunityToolkit.Mvvm.Input;
using GMap.NET.MapProviders;
using Microsoft.Xaml.Behaviors;
using Newtonsoft.Json.Linq;
using NLog.Targets.Wrappers;
using RoSatGCS.Behaviors;
using RoSatGCS.Models;
using System;
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
    #region Types
    public enum BoxType
    {
        None, Boolean, Number, Enumeration, Array
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
            typeof(ValueModel),
            typeof(InputTextBoxBehavior),
            new PropertyMetadata(null));

        public ValueModel ValueModel
        {
            get => (ValueModel)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        protected override void OnAttached()
        {
            ParameterBox.CaretEvent += OnCaretToLast;
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            
        }

        protected override void OnDetaching()
        {
            ParameterBox.CaretEvent -= OnCaretToLast;
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnCaretToLast(object? sender, EventArgs e)
        {
            AssociatedObject.CaretIndex = AssociatedObject.Text.Length;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var control = sender as TextBox;
            if (control == null) { return; }
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

            switch (ValueModel.Parent.InputMethod)
            {
                case InputMethod.BIN:
                    // Only UInt8 or Int8
                    if (!(inputChar == "0" || inputChar == "1") || ValueModel.Value.Length >= 8)
                        e.Handled = true;
                    break;

                case InputMethod.HEX:
                    // Allow 0-9 and A-F
                    if (ValueModel.Value.Replace(" ", "").Length >= 2 * ValueModel.ByteSize || !Regex.IsMatch(inputChar, "^[0-9A-Fa-f]$"))
                    {
                        e.Handled = true;
                        return;
                    }

                    // String Formatter
                    string newText = ValueModel.Value.Insert(control.CaretIndex, e.Text).Replace(" ","");

                    // Format the text with spaces every 2 characters
                    StringBuilder formattedText = new StringBuilder();
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
                    else if (!Regex.IsMatch(inputChar, "^[0-9]$"))
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
                    else if (!Regex.IsMatch(inputChar, "^[0-9]$"))
                    {
                        e.Handled = true;
                    }
                    break;

                case InputMethod.STR:
                    // Accept all input for string
                    break;

                default:
                    e.Handled = true;
                    break;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right)
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
                    ValueModel.Parent.SwapInputMode();
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
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {

            if (item != null && item is ValueModel el)
            {
                if (el.Parent.BoxType == BoxType.Boolean)
                    return BooleanTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent.BoxType == BoxType.Number)
                    return NumberTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent.BoxType == BoxType.Enumeration)
                    return EnumerationTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent.BoxType == BoxType.Array)
                    return ArrayTemplate ?? base.SelectTemplate(item, container);
                if (el.Parent.BoxType == BoxType.None)
                    return NoneTemplate ?? base.SelectTemplate(item, container);
            }

            return base.SelectTemplate(item, container);
        }
    }
    #endregion

    #region Helper
    public class Helper
    {
        public static int GetBaseFromInputMode(InputMethod mode)
        {
            var currentBase = 10;
            if (mode == InputMethod.BIN)
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
                        ret = ret.PadLeft(2, '0').Substring(Math.Max(0, ret.Length - 2));
                    else if (currentBase == 2)
                        ret = ret.PadLeft(8, '0').Substring(Math.Max(0, ret.Length - 8));
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
                default:
                    break;
            }
            if (ret != null && currentBase == 16 && ret.Length % 2 == 1)
                ret = "0" + ret;
            return ret;
        }
    }
    #endregion

    #region ValueModel
    public class ValueModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Notifier
        private readonly Dictionary<string, List<string>> _errors = new();
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public bool HasErrors { get => _errors.Any(); }
        public IEnumerable GetErrors(string? propertyName)
        {
            return propertyName != null && _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Enumerable.Empty<string>();
        }
        private void OnErrorsChanged([CallerMemberName] string? propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }
        public void AddError(string message, [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            _errors[propertyName].Add(message);
            OnErrorsChanged(propertyName); ;
        }
        public void ClearErrors([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
                return;

            _errors.Remove(propertyName);
            OnErrorsChanged(propertyName);
        }

        #endregion

        #region Fields
        private ParameterBox _parent;
        private DataType _dataType;
        private bool _isArray;
        private string _value = "";
        private int _length = 0;
        private int _typesize = 0;
        #endregion

        #region Properties
        public ParameterBox Parent { get { return _parent; } }
        public DataType DataType { get => _dataType; }

        // String Value
        public string Value
        {
            get => _value;
            set { _value = value;
                OnPropertyChanged();
                ValidateValue();
            }
        }

        // Size
        public int Length { get => _length; }
        public int TypeSize { get => _typesize;  }
        public int ByteSize { get => _typesize * _length; }

        public string TypeString
        {
            get
            {
                if(_isArray)
                    return DataType.ToString() + "[" + Length + "]";
                else
                    return DataType.ToString();
            }
        }
       
        public object Result
        {
            get {
                var currentBase = Helper.GetBaseFromInputMode(_parent.InputMethod);

                switch (DataType)
                {
                    case DataType.UInt8:
                        return Convert.ToByte(Value, currentBase);
                    case DataType.Int8:
                        return Convert.ToSByte(Value, currentBase);
                    case DataType.UInt16:
                        return Convert.ToUInt16(Value, currentBase);
                    case DataType.Int16:
                        return Convert.ToInt16(Value, currentBase);
                    case DataType.UInt32:
                        return Convert.ToUInt32(Value, currentBase);
                    case DataType.Integer:
                    case DataType.Int32:
                        return Convert.ToInt32(Value, currentBase);
                    case DataType.UInt64:
                        return Convert.ToUInt64(Value, currentBase);
                    case DataType.Int64:
                        return Convert.ToInt64(Value, currentBase);
                    case DataType.Float:
                        float flt;
                        return float.TryParse(Value, out flt) ? flt : Value;
                    case DataType.Double:
                        double dob;
                        return double.TryParse(Value, out dob) ? dob : Value;
                    case DataType.Boolean:
                    //Todo
                    case DataType.Enumeration:
                    //Todo
                    default:
                        return Value;
                }

            }
        }
        #endregion

        #region Constructor
        public ValueModel(ParameterBox parent, DataType type, bool isArray, int typeSize, int length)
        {
            _parent = parent;
            _dataType = type;
            _isArray = isArray;
            _length = length;
            _typesize = typeSize;
        }
        #endregion

        #region Functions
        public void SwapRefresh(InputMethod prev)
        {
            if (prev == _parent.InputMethod)
                return;

            if(((IEnumerable<string>)GetErrors(nameof(Value))).Count() > 0)
                Value = "";

            var prevBase = Helper.GetBaseFromInputMode(prev);
            var currentBase = Helper.GetBaseFromInputMode(_parent.InputMethod);

            try
            {
                string? ret = Helper.BaseChange(Value, DataType, prevBase, currentBase);
                if (ret == null)
                    AddError("Invalid Conversion", nameof(Value));
                else
                    Value = ret;
            }
            catch
            {
                AddError("Invalid Conversion", nameof(Value));
            }
        }

        private void ValidateValue()
        {
            ClearErrors(nameof(Value)); // Remove old errors

            if (string.IsNullOrWhiteSpace(Value))
                AddError("Value cannot be empty.", nameof(Value));

            if(_parent.InputMethod == InputMethod.HEX && Value.Replace(" ", "").Length % 2 == 1)
                AddError("Hex cannot be odd.", nameof(Value));


            string val = Value;

            try
            {
                if(_isArray)
                {
                    if (_parent.InputMethod == InputMethod.HEX
                        && _typesize * _length * 2 != Value.Replace(" ", "").Length)
                        AddError("Invalid Size", nameof(Value));
                    return;
                }

                var currentBase = Helper.GetBaseFromInputMode(_parent.InputMethod);

                switch (DataType)
                {
                    case DataType.UInt8:
                        Convert.ToByte(val, currentBase);
                        break;
                    case DataType.Int8:
                        Convert.ToSByte(val, currentBase);
                        break;
                    case DataType.UInt16:
                            Convert.ToUInt16(val, currentBase);
                        break;
                    case DataType.Int16:
                            Convert.ToInt16(val, currentBase);
                        break;
                    case DataType.UInt32:
                            Convert.ToUInt32(val, currentBase);
                        break;
                    case DataType.Integer:
                    case DataType.Int32:
                            Convert.ToInt32(val, currentBase);
                        break;
                    case DataType.UInt64:
                            Convert.ToUInt64(val, currentBase);
                        break;
                    case DataType.Int64:
                            Convert.ToInt64(val, currentBase);
                        break;
                    case DataType.Float:
                        if (!float.TryParse(val, out _))
                            throw new ArgumentException();
                        break;
                    case DataType.Double:
                        if (!double.TryParse(val, out _))
                            throw new ArgumentException();
                        break;
                    case DataType.String:
                        break;
                    default:
                        AddError("Invalid Type", nameof(Value));
                        break;
                }
            }
            catch
            {
                AddError("Invalid Value", nameof(Value));
            }
        }

        #endregion
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

        public static readonly DependencyProperty ParameterProperty =
        DependencyProperty.Register(
           "Parameter",
           typeof(ParameterModel),
           typeof(ParameterBox),
           new PropertyMetadata(OnParameterChanged));

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
           "CommandModel",
           typeof(SatelliteCommandModel),
           typeof(ParameterBox),
           new PropertyMetadata(OnCommandModelChanged));


        public static event EventHandler? CaretEvent;

        private static void SetNumberParameterHelper(ParameterBox param, int typeSize, bool isfloat = false, bool includeBinary = false)
        {
            if(param.Parameter.ByteSize % typeSize != 0 || (!param.Parameter.IsArray && param.Parameter.ByteSize != typeSize))
            {
                param.Error = "Invalid Size";
                var v = new ValueModel(param, param.Parameter.DataType, false, typeSize, 1);
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
                if (param.ArrayExpand)
                    param.AvailableInput.Add(InputMethod.DEC);
                param.AvailableInput.Add(InputMethod.HEX);
                if (includeBinary && param.ArrayExpand)
                    param.AvailableInput.Add(InputMethod.BIN);
            }
            param.BoxType = BoxType.Number;

            if (param.ArrayExpand)
            {
                for (int i = 0; i < param.Length; i++)
                {
                    var v = new ValueModel(param, param.Parameter.DataType, false, typeSize, 1);
                    param.ValueModels.Add(v);
                }   
            }
            else
            {
                var v = new ValueModel(param, param.Parameter.DataType, param.IsArray, typeSize, param.Length);
                param.ValueModels.Add(v);
            }
        }

        private static void InitializeParameter(ParameterBox param, bool swapExpand = false)
        {
            List<string> prevValues = new List<string>();
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
                    { param.Error = "Invalid String Array"; return; }
                    param.Length = 1;
                    param.AvailableInput.Add(InputMethod.STR);
                    param.BoxType = BoxType.Array;
                    break;
                case DataType.Boolean:
                    if (param.Parameter.IsArray == true)
                    { param.Error = "Invalid Boolean Array"; return; }
                    param.Length = 1;
                    param.BoxType = BoxType.Boolean;
                    break;
                case DataType.Enumeration:
                    if (param.Parameter.IsArray == true)
                    { param.Error = "Invalid Enumeration Array"; return; }
                    param.Length = 1;
                    param.BoxType = BoxType.Enumeration;
                    break;
                default:
                    { param.Error = "Invalid Data Type"; return; }
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
                InitializeParameter(param);
            }
        }

        private static void OnCommandModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }


        public ParameterModel Parameter
        {
            get => (ParameterModel)GetValue(ParameterProperty);
            set => SetValue(ParameterProperty, value);
        }

        public SatelliteCommandModel CommandModel
        {
            get => (SatelliteCommandModel)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        private ObservableCollection<ValueModel> _valueModels = new ObservableCollection<ValueModel>();
        private List<InputMethod> _availableInput = new List<InputMethod>();
        private BoxType _boxType = BoxType.Number;

        private string _error = "";
        private int _length = 0;
        private int inputMethodIndex = 0;
        private bool _arrayExpand = true;

        public ObservableCollection<ValueModel> ValueModels
        {
            get => _valueModels;
        }

        public List<InputMethod> AvailableInput
        {
            get => _availableInput;
            set => _availableInput = value;
        }

        public InputMethod InputMethod
        {
            get => AvailableInput[inputMethodIndex];
        }

        public BoxType BoxType
        {
            get => _boxType;
            set => _boxType = value;
        }

        public int Length
        {
            get => _length;
            set => _length = value;
        }

        public string Error { get => _error; set => _error = value; }

        public bool ArrayExpand
        {
            get => _arrayExpand;
            set => _arrayExpand = value;
        }

        public string ParameterName
        {
            get => Parameter.Name;
        }

        public string Description
        {
            get => Parameter.Description;
        }

        public List<object> Result
        {
            get
            {
                var list = new List<object>();
                foreach (var item in ValueModels)
                    list.Add(item.Result);
                return list;
            }
        }
        public bool IsArray
        {
            get => Parameter.IsArray;
        }

        public ParameterBox()
        {
            InitializeComponent();
        }

        public void SwapInputMode()
        {
            var prev = InputMethod;
            inputMethodIndex = (inputMethodIndex + 1) % AvailableInput.Count;
            OnPropertyChanged(nameof(InputMethod));
            foreach (var v in _valueModels)
                v.SwapRefresh(prev);

            CaretEvent?.Invoke(this, new EventArgs());
        }
        private void SwapInputMode(object sender, MouseButtonEventArgs? e)
        {
            SwapInputMode();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeParameter(this, true);
        }
    }
}
