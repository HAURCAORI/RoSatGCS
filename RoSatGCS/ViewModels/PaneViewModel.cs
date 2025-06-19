using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.ViewModels
{
    public class PaneViewModel : ViewModelBase
    {
        #region fields
        private int _id;
        private bool _isSelected = false;
        private bool _isActive = false;
        #endregion

        #region constructors
        public PaneViewModel() { }
        #endregion

        #region properties

        public int id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    SetProperty(ref _isSelected, value);
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    SetProperty(ref _isActive, value);
                }
            }
        }
        #endregion
    }
}
