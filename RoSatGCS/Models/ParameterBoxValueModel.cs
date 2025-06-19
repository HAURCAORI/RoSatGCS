using RoSatGCS.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static RoSatGCS.Models.SatelliteFunctionFileModel;


namespace RoSatGCS.Models
{
    #region ValueModel
    // ParameterBox Value Model
    public class ParameterBoxValueModel : INotifyPropertyChanged, INotifyDataErrorInfo, IDisposable
    {
        private readonly static string TrueValue = "true";
        private readonly static string FalseValue = "false";

        #region Notifier
        private readonly Dictionary<string, List<string>> _errors = [];
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public bool HasErrors { get => _errors.Count != 0 && !_isreadonly; }
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
                _errors[propertyName] = [];

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

        private WeakReference<ParameterBox>? _parent;
        public ParameterBox? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                    return target;
                return null;
            }
        }


        private readonly DataType _dataType;
        private readonly bool _isArray;
        private readonly bool _isreadonly = false;
        private readonly int _length = 0;
        private readonly int _typesize = 0;
        private readonly int _id = -1;
        private string _value = "";
        private List<ParameterModel>? _enumerationValues = [];
        private ParameterModel? _selectedEnumItem;
        #endregion

        #region Properties
        public DataType DataType { get => _dataType; }
        public bool IsReadOnly { get => _isreadonly; }
        public bool IsEnabled { get => !_isreadonly; }

        // String Value
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
                ValidateValue();
            }
        }

        // Size
        public int Length { get => _length; }
        public int TypeSize { get => _typesize; }
        public int ByteSize { get => _typesize * _length; }

        public string TypeString
        {
            get
            {
                if (_isArray)
                    return ((DataType != DataType.UserDefined && DataType != DataType.Enumeration) ? DataType.ToString() : "") + "[" + Length + "]";
                else
                {
                    return ((DataType != DataType.UserDefined && DataType != DataType.Enumeration) ? DataType.ToString() : "") + ((Id >= 0) ? (" | " + Id) : "");
                }
            }
        }

        public int Id { get => _id; }

        public List<ParameterModel>? EnumerationValues { get => _enumerationValues; set => _enumerationValues = value; }
        public ParameterModel? SelectedEnumItem { get => _selectedEnumItem; set {
                _selectedEnumItem = value;
                OnPropertyChanged();
                ValidateValue();
            }
        }

        public List<object>? Result
        {
            get
            {
                return ValueConvert();
            }
            set
            {
                Value = ValueConvertBack(value);
            }
        }
        #endregion

        #region Constructor

        private ParameterBoxValueModel()
        {
            _parent = null;
            _dataType = DataType.Integer;
            _isArray = false;
            _length = 0;
            _typesize = 0;
            _isreadonly = false;
            _id = -1;
        }
        public ParameterBoxValueModel(ParameterBox parent, DataType type, bool isArray, int typeSize, int length, bool isReadOnly, int id = -1)
        {
            _parent = new WeakReference<ParameterBox>(parent);
            _dataType = type;
            _isArray = isArray;
            _length = length;
            _typesize = typeSize;
            _isreadonly = isReadOnly;
            _id = id;
        }
        ~ParameterBoxValueModel()
        {
            Dispose(false);
        }
        #endregion

        #region Functions
        public void SwapRefresh(InputMethod prev, InputMethod current)
        {
            if (prev == current)
                return;

            if (((IEnumerable<string>)GetErrors(nameof(Value))).Any())
                Value = "";

            var prevBase = Helper.GetBaseFromInputMode(prev);
            var currentBase = Helper.GetBaseFromInputMode(current);

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

            if (Parent == null)
            {   
                AddError("Invalid Parent.", nameof(Value));
                return;
            }

            if (string.IsNullOrWhiteSpace(Value) && DataType != DataType.Enumeration && DataType != DataType.Boolean && DataType != DataType.String)
            {
                AddError("Value cannot be empty.", nameof(Value));
                Parent.Parameter.HasError = Parent.ValueModels.Any(o => o.HasErrors);
                Parent.Parameter.ValueChangedEvent(this);
                return;
            }
            
            if (Parent.InputMethod == InputMethod.HEX && Value.Replace(" ", "").Length % 2 == 1)
            {
                AddError("Hex cannot be odd.", nameof(Value));
                Parent.Parameter.HasError = Parent.ValueModels.Any(o => o.HasErrors);
                Parent.Parameter.ValueChangedEvent(this);
                return;
            }


            string val = Value;

            try
            {
                if (_isArray)
                {
                    if (Parent.InputMethod == InputMethod.HEX
                        && _typesize * _length * 2 != Value.Replace(" ", "").Length)
                        AddError("Invalid Size", nameof(Value));
                    Parent.Parameter.HasError = Parent.ValueModels.Any(o => o.HasErrors);
                    Parent.Parameter.ValueChangedEvent(this);
                    return;
                }


                var currentBase = Helper.GetBaseFromInputMode(Parent.InputMethod);

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
                        if (Parent.InputMethod == InputMethod.STR
                        && _typesize * _length < Value.Length + 1)
                            AddError("Invalid Size", nameof(Value));
                        break;
                    case DataType.Enumeration:
                        break;
                    case DataType.Boolean:
                        break;
                    case DataType.ByteBuffer:
                        if (_typesize * _length * 2 != Value.Length)
                            AddError("Invalid Size", nameof(Value));
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

            Parent.Parameter.HasError = Parent.ValueModels.Any(o => o.HasErrors);
            Parent.Parameter.ValueChangedEvent(this);
        }

        private List<object>? ValueConvert() {
            if (Parent == null) return null;
            if (Value == null) return null;

            List<string> src = [];
            List<object> ret = [];

            var currentBase = Helper.GetBaseFromInputMode(Parent.InputMethod);

            if (Value != null && Value != string.Empty &&
                ((!Parent.ArrayExpand && Parent.InputMethod != InputMethod.FLT && Parent.InputMethod != InputMethod.STR) ||
                (DataType == DataType.ByteBuffer)))
            {
                // Handling the Single Numerical Input or ByteBuffer

                int hexUnit = (TypeSize * 2);
                if (DataType == DataType.ByteBuffer)
                    hexUnit = 2;

                if (Parent.InputMethod == InputMethod.HEX)
                {
                    string tmp = Value.Replace(" ", "");
                    if (tmp.Length % 2 != 0)
                        return null;

                    tmp = tmp.PadLeft(hexUnit, '0');

                    List<string> hexList = Enumerable.Range(0, tmp.Length / hexUnit).Select(i => tmp.Substring(i * hexUnit, hexUnit)).ToList();
                    src.AddRange(hexList);
                }
                else
                {
                    string? str = Helper.BaseChange(Value, DataType, currentBase, 16);

                    if (str != null)
                    {
                        str = str.PadLeft(hexUnit, '0');
                        src.Add(str);
                        currentBase = 16;
                    }
                }
            }
            else
            {
                // ParameterBox is expanded or single input.
                if (Value != null)
                    src.Add(Value);
            }

            foreach (var item in src)
            {
                switch (DataType)
                {
                    case DataType.UInt8:
                        ret.Add(Convert.ToByte(item, currentBase));
                        break;
                    case DataType.Int8:
                        ret.Add(Convert.ToSByte(item, currentBase));
                        break;
                    case DataType.UInt16:
                        ret.Add(Convert.ToUInt16(item, currentBase));
                        break;
                    case DataType.Int16:
                        ret.Add(Convert.ToInt16(item, currentBase));
                        break;
                    case DataType.UInt32:
                        ret.Add(Convert.ToUInt32(item, currentBase));
                        break;
                    case DataType.Integer:
                    case DataType.Int32:
                        ret.Add(Convert.ToInt32(item, currentBase));
                        break;
                    case DataType.UInt64:
                        ret.Add(Convert.ToUInt64(item, currentBase));
                        break;
                    case DataType.Int64:
                        ret.Add(Convert.ToInt64(item, currentBase));
                        break;
                    case DataType.Float:
                        float flt;
                        if (float.TryParse(item, out flt))
                            ret.Add(flt);
                        break;
                    case DataType.Double:
                        double dob;
                        if (double.TryParse(item, out dob))
                            ret.Add(dob);
                        break;
                    case DataType.Boolean:
                        if (item == TrueValue)
                            ret.Add((byte)0xff);
                        else if (item == FalseValue)
                            ret.Add((byte)0x00);
                        break;
                    case DataType.Enumeration:
                        if (SelectedEnumItem != null)
                            ret.Add((byte)SelectedEnumItem.Id);
                        break;
                    case DataType.String:
                        ret.Add(item);
                        break;
                    case DataType.ByteBuffer:
                        ret.Add(Convert.ToByte(item, currentBase));
                        break;
                    default:
                        return null;
                }
            }

            return ret;
        }

        private string ValueConvertBack(List<object>? src)
        {
            if (Parent == null) return "";
            if (src == null) return "";

            List<string> list = [];

            foreach (var item in src)
            {
                if (item == null)
                {
                    continue;
                }
                else if (item is byte b)
                {
                    if(DataType == DataType.Enumeration)
                    {
                        if (EnumerationValues != null)
                        {
                            var e = EnumerationValues.FirstOrDefault(o => (byte) o.Id == b);
                            SelectedEnumItem = e;
                            continue;
                        }

                    }
                    else if(DataType == DataType.Boolean)
                    {
                        if (b == 0x00)
                            list.Add(FalseValue);
                        else 
                            list.Add(TrueValue);
                    }
                    else
                    {
                        list.Add(b.ToString("X2"));
                    }

                }
                else if (item is sbyte sb)
                {
                    list.Add(sb.ToString("X2"));
                }
                else if (item is ushort us)
                {
                    list.Add(us.ToString("X4"));
                }
                else if (item is short s)
                {
                    list.Add(s.ToString("X4"));
                }
                else if (item is uint ui)
                {
                    list.Add(ui.ToString("X8"));
                }
                else if (item is int i)
                {
                    list.Add(i.ToString("X8"));
                }
                else if (item is ulong ul)
                {
                    list.Add(ul.ToString("X16"));
                }
                else if (item is long l)
                {
                    list.Add(l.ToString("X16"));
                }
                else if (item is float f)
                {
                    list.Add(BitConverter.ToSingle(BitConverter.GetBytes(f), 0).ToString("X8"));
                }
                else if (item is double d)
                {
                    list.Add(BitConverter.ToDouble(BitConverter.GetBytes(d), 0).ToString("X16"));
                }
                else if (item is string str)
                {
                    list.Add(str);
                }
                else if (item is bool bo)
                {
                    if (bo)
                        list.Add(TrueValue);
                    else
                        list.Add(FalseValue);
                }
                else
                {
                    return "";
                }
            }

            var ret = "";
            var currentBase = Helper.GetBaseFromInputMode(Parent.InputMethod);
            if(currentBase == 16)
            {
                ret = string.Join(" ", list);
            }
            else
            {
                foreach(var s in list)
                {
                    ret += Helper.BaseChange(s, DataType, 16, currentBase) + " ";
                }
            }

            ret = ret.Trim();
            if (DataType == DataType.ByteBuffer)
                ret = ret.Replace(" ", "");

            return ret;
        }

        #endregion

        #region Dispose
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                _enumerationValues?.Clear();
                _enumerationValues = null;
                _parent = null;
                _selectedEnumItem = null;
            }

            // Dispose unmanaged resources here (if any)

            _disposed = true;
        }
        #endregion
    }
    #endregion
}
