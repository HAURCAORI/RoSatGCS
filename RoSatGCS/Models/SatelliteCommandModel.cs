using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteCommandModel : SatelliteMethodModel
    {
        #region Fields
        [IgnoreMember]
        private RelayCommand? _execute = null;
        #endregion


        #region Commands
        [IgnoreMember]
        public ICommand Execute { get
            { 
                if(_execute == null)
                    _execute = new RelayCommand(OnExecute);
                return _execute;
            }
        }
        #endregion

        #region Constructors
        private SatelliteCommandModel() : base() { }
        public SatelliteCommandModel(SatelliteMethodModel method) : base(method.Id, method.Visibility, method.File, method.Name, method.Description)
        {
            var copy = (SatelliteMethodModel) method.Clone();
            _methodIn = new List<ParameterModel>(copy.MethodIn);
            _methodOut = new List<ParameterModel>(copy.MethodOut);
            _associatedType = new Dictionary<string, SatelliteFunctionTypeModel>(copy.AssociatedType);
        }

        #endregion

        #region Implementations

        private void OnExecute()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
