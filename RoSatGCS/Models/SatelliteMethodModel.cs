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
    public partial class SatelliteMethodModel : ObservableObject, ICloneable, IEquatable<SatelliteMethodModel>, IDisposable
    {
        #region Fields
        [IgnoreMember]
        private bool _disposed = false;

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
        protected List<ParameterModel> _methodIn = [];
        [Key("methodOut")]
        protected List<ParameterModel> _methodOut = [];
        [Key("associatedType")]
        protected Dictionary<string, SatelliteFunctionTypeModel> _associatedType = [];
        [Key("fidl")]
        protected int _fidlId = 0;
        #endregion

        #region Properties
        [IgnoreMember]
        public int Id { get => _id; set => _id = value; }
        [IgnoreMember]
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        [IgnoreMember]
        public bool Visibility { get => _visibility; internal set => _visibility = value; }
        [IgnoreMember]
        public string Name { get => _name; }
        [IgnoreMember]
        public string File { get => _file; }
        [IgnoreMember]
        public string Description
        {
            get { return (_description == "") ? "-" : _description; }
            set => _description = value;
        }
        [IgnoreMember]
        public List<ParameterModel> MethodIn { get { return _methodIn; } }
        [IgnoreMember]
        public List<ParameterModel> MethodOut { get { return _methodOut; } }
        [IgnoreMember]
        public Dictionary<string, SatelliteFunctionTypeModel> AssociatedType { get => _associatedType; }
        [IgnoreMember]
        public int InSize { get { return MethodIn.Sum(o => o.ByteSize); } }
        [IgnoreMember]
        public int OutSize { get { return MethodOut.Sum(o => o.ByteSize); } }
        [IgnoreMember]
        public int FIDLId { get => _fidlId; set => _fidlId = value; }

        #endregion

        #region Constructors
        protected SatelliteMethodModel() { }
        public SatelliteMethodModel(int id, bool visibility, string file, string name, string description, int fidl)
        {
            _id = id;
            _visibility = visibility;
            _name = name;
            _file = file;
            _description = description;
            _fidlId = fidl;
        }
        ~SatelliteMethodModel()
        {
            Dispose(false);
        }

        #endregion

        #region ETC

        public SatelliteCommandModel GetCommandModel()
        {
            return new SatelliteCommandModel(this);
        }

        public object Clone()
        {
            return new SatelliteMethodModel(this.Id, this.Visibility, this.File, this.Name, this.Description, this.FIDLId)
            {
                _methodIn = this._methodIn.Select(p => (ParameterModel) p.Clone()).ToList(),
                _methodOut = this._methodOut.Select(p => (ParameterModel)p.Clone()).ToList(),
                _associatedType = this._associatedType.ToDictionary(kvp => kvp.Key,
                     kvp => (SatelliteFunctionTypeModel)kvp.Value.Clone())
            };
        }

        public override bool Equals(object? obj) => Equals(obj as SatelliteMethodModel);

        public bool Equals(SatelliteMethodModel? other)
        {
            if (other is null) return false;
            return File == other.File && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(File, Name);
        }

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
                _methodIn.Clear();
                _methodOut.Clear();
            }

            _disposed = true;
        }
        #endregion

    }
}
