using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GCSControls
{
    public class TimeSlotWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 5 ||
            values[0] is not DateTime startTime ||
            values[1] is not DateTime endTime ||
            values[2] is not DateTime timeLineStart ||
            values[3] is not DateTime timeLineEnd ||
            values[4] is not double totalWidth)
                return 0.0;

            var span = (timeLineEnd - timeLineStart).TotalMilliseconds;
            var diff = (endTime - startTime).TotalMilliseconds;

            return (diff / span) * totalWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
