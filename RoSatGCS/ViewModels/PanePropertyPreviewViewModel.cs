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
    public class PanePropertyPreviewViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private WeakReference<PageCommandViewModel> _parent;
        public PageCommandViewModel? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                    return target;
                return null;
            }
        }

        private SatelliteCommandModel? _commandModel;

        public SatelliteCommandModel? CommandModel
        {
            get => _commandModel;
            set
            {
                SetProperty(ref _commandModel, value);
            }
        }

        public ICommand Close { get; set; }

        public PanePropertyPreviewViewModel(PageCommandViewModel viewModel)
        {
            _parent = new WeakReference<PageCommandViewModel>(viewModel);
            Close = new RelayCommand(OnClose);

        }

        private void OnClose()
        {
            if (Parent != null)
                Parent.CloseDocument(this);
        }
    }
}
