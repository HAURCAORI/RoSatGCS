using RoSatGCS.GCSTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoSatGCS.Controls
{
    /// <summary>
    /// StatusIndicator.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StatusIndicator : UserControl
    {
        public StatusIndicator()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty StatusIndicatorProperty = 
            DependencyProperty.Register("Status", typeof(IndicatorStatus), typeof(StatusIndicator), new UIPropertyMetadata(OnChangedCallBack));

        public IndicatorStatus Status
        {
            get { return (IndicatorStatus)GetValue(StatusIndicatorProperty); }
            set { SetValue(StatusIndicatorProperty, value); }
        }

        private static void OnChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is StatusIndicator o)
            {
                switch (e.NewValue)
                {
                    case IndicatorStatus.None:
                        o.Indicator.Background = Brushes.LightGray;
                        break;
                    case IndicatorStatus.Ok:
                        o.Indicator.Background = new BrushConverter().ConvertFrom("#7BEB67") as Brush;
                        break;
                    case IndicatorStatus.Error:
                        o.Indicator.Background = new BrushConverter().ConvertFrom("#F24444") as Brush;
                        break;
                    case IndicatorStatus.Neutral:
                        o.Indicator.Background = new BrushConverter().ConvertFrom("#F2CB05") as Brush;
                        break;
                }
            }
        }
    }
}
