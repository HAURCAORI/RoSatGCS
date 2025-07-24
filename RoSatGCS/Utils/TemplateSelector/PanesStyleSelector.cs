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

                return base.SelectStyle(item, container);
        }
    }
}
