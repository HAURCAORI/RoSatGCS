using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GCSControls
{
    public class TimeSlotXConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 ||
            values[0] is not DateTime startTime ||
            values[1] is not DateTime timeLineStart ||
            values[2] is not DateTime timeLineEnd ||
            values[3] is not double totalWidth)
                return 0.0;

            var span = (timeLineEnd - timeLineStart).TotalMilliseconds;
            var elapsed = (startTime - timeLineStart).TotalMilliseconds;
            return (elapsed / span) * totalWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
