using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static RoSatGCS.Models.SatelliteFunctionFileModel;

namespace RoSatGCS.Views
{
    public class TypeSummaryListTemplateSelector : DataTemplateSelector
    {
        public TypeSummaryListTemplateSelector() { }
        public DataTemplate StructTypeTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item is ParameterModel el)
            {
                if (el.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct)
                {
                    return StructTypeTemplate;
                }
                else if (el.BaseType == SatelliteFunctionTypeModel.ArgumentType.Enum)
                {
                    return EnumTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
