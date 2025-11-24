using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoSatGCS.Behaviors
{
    public static class ChartRedrawBehavior
    {
        // any bound value change will force a redraw
        public static readonly DependencyProperty RedrawTriggerProperty =
            DependencyProperty.RegisterAttached(
                "RedrawTrigger",
                typeof(object),
                typeof(ChartRedrawBehavior),
                new PropertyMetadata(null, OnRedrawTriggerChanged));

        public static void SetRedrawTrigger(DependencyObject element, object value)
            => element.SetValue(RedrawTriggerProperty, value);

        public static object GetRedrawTrigger(DependencyObject element)
            => element.GetValue(RedrawTriggerProperty);

        private static void OnRedrawTriggerChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // works for any LiveCharts IChartView (CartesianChart, PieChart, etc.)
            if (d is IChartView chart && chart.CoreChart is not null)
            {
                var series = chart.Series;
                series.Count();

                chart.Invalidate();
            }
        }
    }
}
