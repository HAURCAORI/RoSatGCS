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


        public DataTemplate? CommandFilePaneTemplate { get; set; }
        public DataTemplate? TypeDictionaryPaneTemplate { get; set; }
        public DataTemplate? FunctionListPaneTemplate { get; set; }
        public DataTemplate? FunctionPropertyPaneTemplate { get; set; }
        public DataTemplate? CommandSetPaneTemplate { get; set; }
        public DataTemplate? TypeSummaryPaneTemplate { get; set; }
        public DataTemplate? PropertyPreviewPaneTemplate { get; set; }
        public DataTemplate? TleListPaneTemplate { get; set; }
        public DataTemplate? PassCommnadPaneTemplate { get; set; }
        public DataTemplate? PassQueuePaneTemplate { get; set; }
        public DataTemplate? PassSchedulePaneTemplate { get; set; }
        public DataTemplate? PassTimelinePaneTemplate { get; set; }


        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            var itemAsLayoutContent = item as LayoutContent;

            if (item is PaneCommandFileViewModel)
                return CommandFilePaneTemplate ?? new DataTemplate();
            else if (item is PaneTypeDictionaryViewModel)
                return TypeDictionaryPaneTemplate ?? new DataTemplate();
            else if (item is PaneFunctionListViewModel)
                return FunctionListPaneTemplate ?? new DataTemplate();
            else if (item is PaneFunctionPropertyViewModel)
                return FunctionPropertyPaneTemplate ?? new DataTemplate();
            else if (item is PaneCommandSetViewModel)
                return CommandSetPaneTemplate ?? new DataTemplate();
            else if (item is PaneTypeSummaryViewModel)
                return TypeSummaryPaneTemplate ?? new DataTemplate();
            else if (item is PanePropertyPreviewViewModel)
                return PropertyPreviewPaneTemplate ?? new DataTemplate();
            else if (item is PaneTleListViewModel)
                return TleListPaneTemplate ?? new DataTemplate();
            else if (item is PanePassCommandViewModel)
                return PassCommnadPaneTemplate ?? new DataTemplate();
            else if (item is PanePassQueueViewModel)
                return PassQueuePaneTemplate ?? new DataTemplate();
            else if (item is PanePassScheduleViewModel)
                return PassSchedulePaneTemplate ?? new DataTemplate();
            else if (item is PanePassTimelineViewModel)
                return PassTimelinePaneTemplate ?? new DataTemplate();

            return base.SelectTemplate(item, container);
        }
    }
}
