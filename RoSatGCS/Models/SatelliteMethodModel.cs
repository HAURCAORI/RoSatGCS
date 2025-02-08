using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteMethodModel : ObservableObject, ICloneable
    {
        #region Fields
        [Key("id")]
        protected int _id = 0;
        [Key("isSelected")]
        protected bool _isSelected = false;
        [Key("visibility")]
        protected bool _visibility = true;
        [Key("name")]
        protected string _name = "";
        [Key("file")]
        protected string _file = "";
        [Key("description")]
        protected string _description = "";
        [Key("methodIn")]
        protected List<ParameterModel> _methodIn = new List<ParameterModel>();
        [Key("methodOut")]
        protected List<ParameterModel> _methodOut = new List<ParameterModel>();
        [Key("associatedType")]
        protected Dictionary<string, SatelliteFunctionTypeModel> _associatedType = new Dictionary<string, SatelliteFunctionTypeModel>();
        #endregion

        #region Properties
        [Key("Id")]
        public int Id { get => _id; set => _id = value; }
        [Key("IsSelected")]
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        [Key("Visibility")]
        public bool Visibility { get => _visibility; internal set => _visibility = value; }
        [Key("Name")]
        public string Name { get => _name; internal set => _name = value; }
        [Key("File")]
        public string File { get => _file; internal set => _file = value; }
        [Key("Description")]
        public string Description
        {
            get { return (_description == "") ? "-" : _description; }
            set => _description = value;
        }
        [Key("MethodIn")]
        public List<ParameterModel> MethodIn { get { return _methodIn; } }
        [Key("MethodOut")]
        public List<ParameterModel> MethodOut { get { return _methodOut; } }
        [Key("AssociatedType")]
        public Dictionary<string, SatelliteFunctionTypeModel> AssociatedType { get => _associatedType; }

        #endregion

        #region Constructors
        protected SatelliteMethodModel() { }
        public SatelliteMethodModel(int id, bool visibility, string file, string name, string description)
        {
            _id = id;
            _visibility = visibility;
            _name = name;
            _file = file;
            _description = description;
        }
        #endregion

        #region ETC

        public SatelliteCommandModel GetCommandModel()
        {
            return new SatelliteCommandModel(this);
        }

        public object Clone()
        {
            return new SatelliteMethodModel(this.Id, this.Visibility, this.File, this.Name, this.Description)
            {
                _methodIn = this._methodIn.Select(p => (ParameterModel) p.Clone()).ToList(),
                _methodOut = this._methodOut.Select(p => (ParameterModel)p.Clone()).ToList(),
                _associatedType = this._associatedType.ToDictionary(kvp => kvp.Key,
                     kvp => (SatelliteFunctionTypeModel)kvp.Value.Clone())
            };
        }
        #endregion

    }
}
