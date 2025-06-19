using Accessibility;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static RoSatGCS.Models.SatelliteFunctionTypeModel;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteFunctionTypeModel : ObservableObject, ICloneable
    {
        public enum ArgumentType
        {
            None, Struct, Enum
        }

        #region Fields
        [Key("vis")]
        private bool _visibility = true;
        [Key("size")]
        private int _size = 0;
        [Key("name")]
        private string _name = "";
        [Key("file")]
        private string _file = "";
        [Key("desc")]
        private string _description = "";
        [Key("param")]
        private List<ParameterModel> _parameters = new List<ParameterModel>();
        [Key("type")]
        private ArgumentType type;

        #endregion

        #region Properties
        [IgnoreMember]
        public bool Visibility { get => _visibility; internal set => _visibility = value; }
        [IgnoreMember]
        public int Size { get => _size; internal set => _size = value; }
        [IgnoreMember]
        public string Name { get => _name; internal set => _name = value; }
        [IgnoreMember]
        public string File { get => _file; internal set => _file = value; }
        [IgnoreMember]
        public string Identifier { get { return (Name + "[" + File + "]"); } }
        [IgnoreMember]
        public string Description
        {
            get
            {
                if (_description == "")
                    return "-";
                return _description;
            }
            set => _description = value;
        }
        [IgnoreMember]
        public ArgumentType Type { get => type; internal set => type = value; }
        [IgnoreMember]
        public List<ParameterModel> Parameters { get { return _parameters; } }

        #endregion

        #region Constructors
        private SatelliteFunctionTypeModel() {}

        public SatelliteFunctionTypeModel(ArgumentType type, string file, string name, string description) {
            Type = type;
            _name = name;
            _file = file;
            _description = description;
        }
        #endregion

        #region ETC
        public object Clone()
        {
            return new SatelliteFunctionTypeModel(this.Type, this.File, this.Name, this.Description)
            {
                _visibility = this.Visibility,
                Size = this.Size,
                _parameters = this.Parameters.Select(p=> (ParameterModel)p.Clone()).ToList()
            };
        }
        #endregion
    }
}
