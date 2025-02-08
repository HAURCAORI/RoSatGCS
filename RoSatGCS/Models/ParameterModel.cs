using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RoSatGCS.Models.SatelliteFunctionTypeModel;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class ParameterModel : ObservableObject, ICloneable
    {
        #region Fields
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
        #endregion
        
        #region Properties
        // General Properties
        [IgnoreMember]
        public ArgumentType BaseType { get => _baseType; }
        [IgnoreMember]
        public string Name { get => _name; internal set => _name = value; }
        [IgnoreMember]
        public string File { get => _file; internal set => _file = value; }
        [IgnoreMember]
        public string Description { get => _description; internal set => _description = value; }
        [IgnoreMember]
        public int Id { get => _id; internal set => _id = value; }

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
        public string DataTypeString
        {
            get
            {
                if (IsArray)
                    if (IsUserDefined)
                        return UserDefinedType + "[ ]";
                    else
                        return DataType.ToString() + "[ ]";
                else
                    if (IsUserDefined)
                    return UserDefinedType;
                else
                    return DataType.ToString();
            }
        }
        #endregion

        #region Constructors
        private ParameterModel() { }
        public ParameterModel(ArgumentType type, string file, string name, string description)
        {
            _baseType = type;
            _name = name;
            _file = file;
            _description = description;
        }
        #endregion

        #region ETC
        public object Clone()
        {
            return new ParameterModel(this.BaseType, this.File, this.Name, this.Description)
            {
                Id = this.Id,
                Index = this.Index,
                ByteSize = this.ByteSize,
                DataType = this.DataType,
                UserDefinedType = this.UserDefinedType,
                IsArray = this.IsArray
            };
        }
        #endregion
    }
}
