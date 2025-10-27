using AvalonDock.Controls;
using RoSatGCS.ViewModels;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;

namespace RoSatGCS.Views
{
    public class PanesStyleSelector : StyleSelector
    {
        public Style? CommandFilePaneStyle { get; set; }
        public Style? TypeDictionaryPaneStyle { get; set; }
        public Style? FunctionListPaneStyle { get; set; }
        public Style? FunctionPropertyPaneStyle { get; set; }
        public Style? CommandSetPaneStyle { get; set; }
        public Style? TypeSummaryPaneStyle { get; set; }
        public Style? PropertyPreviewPaneStyle { get; set; }
        public Style? TLEListPaneStyle { get; set; }
        public Style? PassCommandPaneStyle { get; set; }
        public Style? PassQueuePaneStyle { get; set; }
        public Style? PassSchedulePaneStyle { get; set; }
        public Style? PassTimelinePaneStyle { get; set; }
        public Style? PlotWindowPaneStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is PaneCommandFileViewModel)
                return CommandFilePaneStyle ?? new Style();
            else if (item is PaneTypeDictionaryViewModel)
                return TypeDictionaryPaneStyle ?? new Style();
            else if (item is PaneFunctionListViewModel)
                return FunctionListPaneStyle ?? new Style();
            else if (item is PaneFunctionPropertyViewModel)
                return FunctionPropertyPaneStyle ?? new Style();
            else if (item is PaneCommandSetViewModel)
                return CommandSetPaneStyle ?? new Style();
            else if (item is PaneTypeSummaryViewModel)
                return TypeSummaryPaneStyle ?? new Style();
            else if (item is PanePropertyPreviewViewModel)
                return PropertyPreviewPaneStyle ?? new Style();
            else if (item is PaneTleListViewModel)
                return TLEListPaneStyle ?? new Style();
            else if (item is PanePassCommandViewModel)
                return PassCommandPaneStyle ?? new Style();
            else if (item is PanePassQueueViewModel)
                return PassQueuePaneStyle ?? new Style();
            else if (item is PanePassScheduleViewModel)
                return PassSchedulePaneStyle ?? new Style();
            else if (item is PanePassTimelineViewModel)
                return PassTimelinePaneStyle ?? new Style();
            else if (item is PanePlotWindowViewModel)
                return PlotWindowPaneStyle ?? new Style();

            return base.SelectStyle(item, container);
        }
    }
}
