using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoSatGCS.Controls
{
    /// <summary>
    /// ParameterView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ParameterView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly DependencyProperty ParameterProperty =
        DependencyProperty.Register(
           "Parameter",
           typeof(ParameterModel),
           typeof(ParameterView),
           new PropertyMetadata(OnParameterChanged));

        public ParameterModel Parameter
        {
            get => (ParameterModel)GetValue(ParameterProperty);
            set => SetValue(ParameterProperty, value);
        }
        private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ParameterView param)
            {
                param.Parameter.Received += param.OnReceived;
                param.ParameterModel = param.Parameter;
            }
        }

        public ParameterModel ParameterModel
        {
            get => Parameter;
            set { Parameter = value; OnPropertyChanged(); }
        }

        public string ParameterName
        {
            get => ParameterModel.Name;
        }

        public void OnReceived(object? sender, EventArgs e)
        {
            ParameterModel = Parameter;
        }

        public ParameterView()
        {
            InitializeComponent();
        }
        ~ParameterView()
        {
            Parameter.Received -= OnReceived;
        }
    }
}
