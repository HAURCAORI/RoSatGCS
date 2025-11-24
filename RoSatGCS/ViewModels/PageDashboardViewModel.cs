using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PageDashboardViewModel : ViewModelPageBase
    {
        private readonly ObservableCollection<PaneViewModel> _anchorable = [];
        private readonly ObservableCollection<PaneViewModel> _document = [];

        public ObservableCollection<PaneViewModel> Anchorable { get => _anchorable; }
        public ObservableCollection<PaneViewModel> DocumentPane { get => _document; }



        public PlotDataContainer PlotData { get => MainDataContext.Instance.PlotDataContainer; }
        
        public PageDashboardViewModel()
        {

            Anchorable.Add(new PanePlotWindowViewModel());
        }
    }
}
