using AdonisUI.Controls;
using RoSatGCS.Utils.Satellites.TLE;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RoSatGCS.Views
{
    /// <summary>
    /// WindowTLE.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WindowTLE : AdonisWindow
    {
        public WindowTLE()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(WindowTLEViewModel));
        }

        public void Initialize(TLE tle, Action<TLE> onClose)
        {
            if (DataContext is WindowTLEViewModel viewModel)
            {
                viewModel.Initialize(tle);
            }
            this.Closed += (s, e) => 
            {
                if (DataContext is WindowTLEViewModel viewModel)
                {
                    if (viewModel.TLEData != null && viewModel.TLEData.IsValid)
                    {
                        onClose?.Invoke(viewModel.TLEData);
                    }
                }
            };
        }
    }
}
