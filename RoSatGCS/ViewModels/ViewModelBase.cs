using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using RoSatGCS.Utils.Navigation;

namespace RoSatGCS.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        #region fields
        private string _title = "";
        #endregion

        #region properties

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        #endregion
    }
}
