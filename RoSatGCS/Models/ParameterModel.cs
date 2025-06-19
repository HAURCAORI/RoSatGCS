using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
using Newtonsoft.Json.Linq;
using RoSatGCS.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static RoSatGCS.Models.SatelliteCommandModel;
using static RoSatGCS.Models.SatelliteFunctionTypeModel;

namespace RoSatGCS.Models
{
    // Satellite Function File Parameter
    [MessagePackObject(AllowPrivate = true)]
    public partial class ParameterModel : ObservableObject, ICloneable, IDisposable
    {
        [IgnoreMember]
        public ICommand? PullValue { get; set; }

        public event EventHandler? Received;

        public void ReceivedEvent(object? o)
        {
            if(_value == null) { return; }
            Received?.Invoke(o, new EventArgs());
        }

        public event EventHandler? ValueChanged;

        public void ValueChangedEvent(object? o)
        {
            if(_init == true)
                ValueChanged?.Invoke(o,new EventArgs());
        }



        #region Fields
        [IgnoreMember]
        private WeakReference<SatelliteCommandModel>? _model;
        [Key("baseType")]
        private ArgumentType _baseType;
        [Key("name")]
        private string _name = "";
        [Key("file")]
        private string _file = "";
        [Key("description")]
        private string _description = "";
        [Key("id")]
        private int _id = 0;
        [Key("index")]
        private int _index = 0;
        [Key("byteSize")]
        private int _byteSize = 0;
        [Key("udt")]
        private string _userDefinedType = "";
        [Key("dataType")]
        private SatelliteFunctionFileModel.DataType _dataType;
        [Key("isArray")]
        private bool _isArray = false;
        [IgnoreMember]
        private bool _isReadOnly = false;
        [IgnoreMember]
        private string _seuence = "";
        [Key("value")]
        private List<object>? _value = new();
        [Key("hasError")]
        private bool _hasError = true;
        [IgnoreMember]
        private bool _init = false;
        #endregion

        #region Properties
        [IgnoreMember]
        public SatelliteCommandModel? CommandModel {
            get
            {
                if (_model != null && _model.TryGetTarget(out var target))
                    return target;
                return null;
            }
            set
            {
                if(value != null)
                    _model = new WeakReference<SatelliteCommandModel>(value);
            }
        }

        // General Properties
        [IgnoreMember]
        public ArgumentType BaseType { get => _baseType; internal set => _baseType = value; }
        [IgnoreMember]
        public string Name { get => _name; internal set => _name = value; }
        [IgnoreMember]
        public string File { get => _file; internal set => _file = value; }
        [IgnoreMember]
        public string Description { get => _description; internal set => _description = value; }
        [IgnoreMember]
        public int Id { get => _id; internal set => _id = value; }
        [IgnoreMember]
        public string Sequence {  get => _seuence; internal set => _seuence = value; } // Used for function property view model
        [IgnoreMember]
        public List<object>? Value { get => _value; internal set => _value = value;  }
        [IgnoreMember]
        public bool HasError { get => _hasError; internal set => _hasError = value; }
        [IgnoreMember]
        public bool Init { get => _init; internal set => _init = value; }


        // Enum Only Properties
        [IgnoreMember]
        public string EnumValue { get => Name + "(" + Id + ")"; }

        // Struct Only Properties
        [IgnoreMember]
        public int Index { get => _index; internal set => _index = value; }
        [IgnoreMember]
        public int ByteSize { get => _byteSize; internal set => _byteSize = value; }
        [IgnoreMember]
        public SatelliteFunctionFileModel.DataType DataType { get => _dataType; internal set => _dataType = value; }
        [IgnoreMember]
        public string UserDefinedType { get => _userDefinedType; internal set => _userDefinedType = value; }
        [IgnoreMember]
        public bool IsUserDefined { get { return DataType == SatelliteFunctionFileModel.DataType.UserDefined; } }
        [IgnoreMember]
        public bool IsArray { get => _isArray; internal set => _isArray = value; }
        [IgnoreMember]
        public bool IsReadOnly { get => _isReadOnly; internal set => _isReadOnly = value; }
        [IgnoreMember]
        public string DataTypeString
        {
            get
            {
                if (DataType == SatelliteFunctionFileModel.DataType.Enumeration)
                    return UserDefinedType;
                else if (IsArray)
                    if (IsUserDefined)
                        return UserDefinedType + "[ ]";
                    else
                        return DataType.ToString() + "[ ]";
                else if (IsUserDefined)
                    return UserDefinedType;
                else
                    return DataType.ToString();
            }
        }
        #endregion

        public static List<object> ConvertValue(SatelliteFunctionFileModel.DataType dataType, List<byte> bytes, ArgumentType type = ArgumentType.None)
        {
            List<object> value = [];
            switch (dataType)
            {
                case SatelliteFunctionFileModel.DataType.Boolean:
                    for (int i = 0; i < bytes.Count; i += 1)
                        value.Add(BitConverter.ToBoolean([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.Int8:
                    for (int i = 0; i < bytes.Count; i += 1)
                        value.Add(bytes[i]);
                    break;
                case SatelliteFunctionFileModel.DataType.Int16:
                    for (int i = 0; i < bytes.Count; i += 2)
                        value.Add(BitConverter.ToInt16([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.Int32:
                    for (int i = 0; i < bytes.Count; i += 4)
                        value.Add(BitConverter.ToInt32([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.Int64:
                    for (int i = 0; i < bytes.Count; i += 8)
                        value.Add(BitConverter.ToInt64([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.UInt8:
                    for (int i = 0; i < bytes.Count; i += 1)
                        value.Add(bytes[i]);
                    break;
                case SatelliteFunctionFileModel.DataType.UInt16:
                    for (int i = 0; i < bytes.Count; i += 2)
                        value.Add(BitConverter.ToUInt16([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.UInt32:
                    for (int i = 0; i < bytes.Count; i += 4)
                        value.Add(BitConverter.ToUInt32([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.UInt64:
                    for (int i = 0; i < bytes.Count; i += 8)
                        value.Add(BitConverter.ToUInt64([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.Integer:
                    for (int i = 0; i < bytes.Count; i += 4)
                        value.Add(BitConverter.ToInt32([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.Float:
                    for (int i = 0; i < bytes.Count; i += 4)
                        value.Add(BitConverter.ToSingle([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.Double:
                    for (int i = 0; i < bytes.Count; i += 8)
                        value.Add(BitConverter.ToDouble([.. bytes], i));
                    break;
                case SatelliteFunctionFileModel.DataType.String:
                    value.Add(Encoding.ASCII.GetString(bytes.ToArray()).Trim('\0'));
                    break;
                case SatelliteFunctionFileModel.DataType.ByteBuffer:
                    foreach (var item in bytes)
                        value.Add(item);
                    break;
                case SatelliteFunctionFileModel.DataType.Enumeration:
                    foreach (var item in bytes)
                        value.Add(item);
                    break;
                case SatelliteFunctionFileModel.DataType.UserDefined:
                    if (type != ArgumentType.Enum)
                    {
                        throw new NotImplementedException();
                    }
                    foreach (var item in bytes)
                        value.Add(item);
                    break;
            }
            return value;
        }

        public static List<ParameterModel> GetType(ParameterModel pm)
        {
            var list = new List<ParameterModel>();

            var param = (ParameterModel)pm.Clone();

            if (param.CommandModel == null || !param.IsUserDefined)
            {
                list.Add(param);
                return list;
            }

            if (param.CommandModel.AssociatedType.TryGetValue(param.UserDefinedType, out SatelliteFunctionTypeModel? tmp))
            {
                var type = (SatelliteFunctionTypeModel)tmp.Clone();
                if (type.Type == SatelliteFunctionTypeModel.ArgumentType.Struct)
                {
                    // Add Header
                    var header = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.Struct, type.File, type.Name, type.Description)
                    {
                        CommandModel = param.CommandModel,
                        ByteSize = param.ByteSize,
                        DataType = SatelliteFunctionFileModel.DataType.None,
                        Sequence = param.Sequence
                    };
                    list.Add(header);

                    var len = 1;
                    if (param.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct && param.IsArray)
                    {
                        len = param.ByteSize / type.Size;
                    }

                    for (int i = 0; i < len; i++)
                    {

                        var index = 1;
                        foreach (var p in type.Parameters)
                        {
                            var p_copy = (ParameterModel)p.Clone();
                            p_copy.CommandModel = param.CommandModel;
                            p_copy.IsReadOnly = param.IsReadOnly;
                            p_copy.Sequence = param.Sequence + "." + (index++).ToString();

                            if (p_copy.IsUserDefined)
                            {
                                list.AddRange(GetType(p_copy));
                            }
                            else
                            {
                                list.Add(p_copy);
                            }
                        }
                    }
                }
                else if (type.Type == SatelliteFunctionTypeModel.ArgumentType.Enum)
                {
                    param.DataType = SatelliteFunctionFileModel.DataType.Enumeration;
                    list.Add(param);
                }
            }
            else
            {
                list.Add(param);
            }

            return list;
        }

        #region Constructors
        private ParameterModel() { }
        public ParameterModel(ArgumentType type, string file, string name, string description)
        {
            _baseType = type;
            _name = name;
            _file = file;
            _description = description;
        }
        ~ParameterModel()
        {
            Dispose();
        }
        #endregion

        #region ETC
        public void Dispose()
        {
            if (Received != null)
            {
                foreach (Delegate d in Received.GetInvocationList())
                {
                    Received -= (EventHandler)d;
                }
            }
            GC.SuppressFinalize(this); // Prevents finalizer from running
        }

        public object Clone()
        {
            return new ParameterModel(this.BaseType, this.File, this.Name, this.Description)
            {
                Id = this.Id,
                Index = this.Index,
                ByteSize = this.ByteSize,
                DataType = this.DataType,
                UserDefinedType = this.UserDefinedType,
                IsArray = this.IsArray,
                CommandModel = this.CommandModel,
                IsReadOnly = this.IsReadOnly,
                Sequence = this.Sequence
            };
        }
        #endregion
    }
}
