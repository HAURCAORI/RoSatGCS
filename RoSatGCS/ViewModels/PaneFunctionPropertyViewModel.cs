using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneFunctionPropertyViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private PageCommandViewModel _parent;

        private bool _isParametersVisible = false;
        private bool _isResultsVisible = false;
        private int _column = 2;
        private SatelliteCommandModel _command;
        
        public bool IsParametersVisible { get => HasInput && _isParametersVisible; set => SetProperty(ref _isParametersVisible, value); }
        public bool IsResultsVisible { get => HasOutput && _isResultsVisible; set => SetProperty(ref _isResultsVisible, value); }
        public bool HasInput { get => _command.MethodIn.Count > 0; }
        public bool HasOutput { get => _command.MethodOut.Count > 0; }
        public int GridColumn { get { return _column; } set { SetProperty(ref _column, value); } }
        public SatelliteCommandModel Command { get => _command; set => _command = value; }
        

        public ICommand Close { get; set; }
        public ICommand ParametersClick { get; set; }
        public ICommand ResultsClick { get; set; }
        public ICommand SizeChanged { get; set; }


        public PaneFunctionPropertyViewModel(PageCommandViewModel viewModel, SatelliteMethodModel command)
        {
            _parent = viewModel;
            _command = new SatelliteCommandModel(command);

            Close = new RelayCommand(OnClose);
            ParametersClick = new RelayCommand(OnParametersClick);
            ResultsClick = new RelayCommand(OnResultsClick);
            SizeChanged = new RelayCommand<object>(OnSizeChanged);
        }

        private void OnClose()
        {
            _parent.CloseDocument(this);
        }

        private void OnParametersClick()
        {
            IsParametersVisible = !IsParametersVisible;
        }

        private void OnResultsClick()
        {
            IsResultsVisible = !IsResultsVisible;
        }

        private void OnSizeChanged(object? sender)
        {
            if (sender is FrameworkElement args)
            {
                GridColumn = (int)(args.ActualWidth / 250);
            }
        }
    }
}
