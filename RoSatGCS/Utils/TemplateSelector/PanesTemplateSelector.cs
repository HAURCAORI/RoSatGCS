using AvalonDock.Layout;
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
    public class PanesTemplateSelector : DataTemplateSelector
    {
        public PanesTemplateSelector() { }


        public DataTemplate CommandFilePaneTemplate { get; set; }
        public DataTemplate TypeDictionaryPaneTemplate { get; set; }
        public DataTemplate FunctionListPaneTemplate { get; set; }
        public DataTemplate FunctionPropertyPaneTemplate { get; set; }
        public DataTemplate CommandSetPaneTemplate { get; set; }
        public DataTemplate TypeSummaryPaneTemplate { get; set; }
        public DataTemplate PropertyPreviewPaneTemplate { get; set; }


        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            var itemAsLayoutContent = item as LayoutContent;

            if (item is PaneCommandFileViewModel)
                return CommandFilePaneTemplate;
            else if (item is PaneTypeDictionaryViewModel)
                return TypeDictionaryPaneTemplate;
            else if (item is PaneFunctionListViewModel)
                return FunctionListPaneTemplate;
            else if (item is PaneFunctionPropertyViewModel)
                return FunctionPropertyPaneTemplate;
            else if (item is PaneCommandSetViewModel)
                return CommandSetPaneTemplate;
            else if (item is PaneTypeSummaryViewModel)
                return TypeSummaryPaneTemplate;
            else if (item is PanePropertyPreviewViewModel)
                return PropertyPreviewPaneTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
