using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GCSControls
{
    internal class TimeSlotTemplateSelector : DataTemplateSelector
    {
        public TimeSlotTemplateSelector() { }

        public DataTemplate? PriorityHighTemplate { get; set; }
        public DataTemplate? PriorityMidTemplate { get; set; }
        public DataTemplate? PriorityLowTemplate { get; set; }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {

            DataTemplate? template = null;
            if (item is TimeSlotPriority priority)
            {
                switch (priority)
                {
                    case TimeSlotPriority.High:
                        template = PriorityHighTemplate;
                        break;
                    case TimeSlotPriority.Mid:
                        template = PriorityMidTemplate;
                        break;
                    case TimeSlotPriority.Low:
                        template = PriorityLowTemplate;
                        break;
                }
            }

            return template ?? base.SelectTemplate(item, container);
        }
    }
}
