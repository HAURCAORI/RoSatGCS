using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneCommandSetViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Fields
        private string? _groupAddName;
        private RelayCommand? _groupAdd;
        private RelayCommand? _deleteAll;
        #endregion

        #region Properties
        public string? GroupAddName
        {
            get => _groupAddName;
            set => SetProperty(ref _groupAddName, value);
        }

        #endregion

        #region Commands

        public ICommand GroupAdd { get => _groupAdd ??= new RelayCommand(OnGroupAdd); }
        public ICommand DeleteAll { get => _deleteAll ??= new RelayCommand(OnDeleteAll); }

        #endregion

        public PaneCommandSetViewModel() { }

        private void OnGroupAdd()
        {
            var Parent = MainDataContext.Instance.GetPageCommandViewModel;
            if (Parent == null) { return; }
            if (GroupAddName == null) { return; }
            if (GroupAddName.Trim() == string.Empty) { return; }
            var newGroup = new SatelliteCommandGroupModel(GroupAddName.Trim());
            Parent.GroupAdd.Execute(newGroup);
            GroupAddName = null;
        }

        private void OnDeleteAll()
        {
            var Parent = MainDataContext.Instance.GetPageCommandViewModel;
            if (Parent == null) { return; }
            if (MainDataContext.Instance.SatelliteCommandGroup.Count == 0) { return; }
            Parent.DeleteCommandGroupAll();
        }


    }
}
