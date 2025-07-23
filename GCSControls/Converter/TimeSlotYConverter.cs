using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GCSControls
{
    public class TimeSlotYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 ||
            values[0] is not TimeSlotItem item ||
            values[1] is not ObservableCollection<TimeSlotItem> list ||
            values[2] is not double offset)
                return 0.0;


            int index = list.IndexOf(item);
            if (index >= 0)
            {
                return index * TimeSlots.kTimeSlotHeight - offset + 1;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
