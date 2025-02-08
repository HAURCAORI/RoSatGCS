using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.ViewModels
{
    public class PaneCommandSetViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private PageCommandViewModel _parent;

        public PaneCommandSetViewModel(PageCommandViewModel viewModel)
        {
            _parent = viewModel;
        }
    }
}
