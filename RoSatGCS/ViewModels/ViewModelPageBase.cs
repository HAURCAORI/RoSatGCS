using RoSatGCS.Utils.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.ViewModels
{
    public abstract class ViewModelPageBase : ViewModelBase, INavigationAware
    {
        public void OnNavigated(object sender, object navigatedEventArgs) { }

        public void OnNavigating(object sender, object navigationEventArgs) { }
    }
}
