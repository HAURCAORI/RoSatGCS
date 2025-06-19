using AvalonDock.Layout;
using RoSatGCS.Models;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RoSatGCS.Views
{
    public class ShowHideTemplateSelector : DataTemplateSelector
    {
        public ShowHideTemplateSelector() { }

        public DataTemplate ShowTemplate { get; set; }
        public DataTemplate HideTemplate { get; set; }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            var itemAsLayoutContent = item as LayoutContent;

            if (item is bool visible)
            {
                if (visible == true)
                    return ShowTemplate;
                else
                    return HideTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
