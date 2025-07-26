using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneTypeSummaryViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private int _column = 2;
        private SatelliteFunctionTypeModel? _satFuncType;

        public SatelliteFunctionTypeModel? SatFuncType
        {
            get { return _satFuncType; }
            set { SetProperty(ref _satFuncType, value); }
        }

        public int GridColumn { get { return _column; } set { SetProperty(ref _column, value); } }
        public ICommand Close { get; set; }
        public ICommand SizeChanged { get; set; }


        public PaneTypeSummaryViewModel()
        {
            Close = new RelayCommand(OnClose);
            SizeChanged = new RelayCommand<object>(OnSizeChanged);

        }

        private void OnClose()
        {
            var Parent = MainDataContext.Instance.GetPageCommandViewModel;
            if (Parent != null)
                Parent.CloseDocument(this);
        }
        private void OnSizeChanged(object? sender)
        {
            if (sender is FrameworkElement args)
            {
                GridColumn = (int) (args.ActualWidth / 250);
            }
        }
        
    }
}
