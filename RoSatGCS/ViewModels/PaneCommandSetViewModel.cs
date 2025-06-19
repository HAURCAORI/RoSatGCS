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

        private readonly WeakReference<PageCommandViewModel>? _parent;
        public PageCommandViewModel? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                    return target;
                return null;
            }
        }

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

        private PaneCommandSetViewModel() { }
        public PaneCommandSetViewModel(PageCommandViewModel viewModel)
        {
            _parent = new WeakReference<PageCommandViewModel>(viewModel);
        }

        private void OnGroupAdd()
        {
            if (Parent == null) { return; }
            if (GroupAddName == null) { return; }
            if (GroupAddName.Trim() == string.Empty) { return; }
            var newGroup = new SatelliteCommandGroupModel(Parent, GroupAddName.Trim());
            Parent.GroupAdd.Execute(newGroup);
            GroupAddName = null;
        }

        private void OnDeleteAll()
        {
            if (Parent == null) { return; }
            if (Parent.SatelliteCommandGroup.Count == 0) { return; }
            Parent.DeleteCommandGroupAll();
        }


    }
}
