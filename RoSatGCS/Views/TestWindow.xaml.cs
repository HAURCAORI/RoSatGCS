using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;
using NLog;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
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
using System.Windows.Shapes;

namespace RoSatGCS.Views
{
    /// <summary>
    /// TestWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TestWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private ParameterModel? _parameter;
        private SatelliteCommandModel? _commandModel;

        public ParameterModel? Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value;
                OnPropertyChanged();
            }
        }

        public SatelliteCommandModel? CommandModel
        {
            get => _commandModel;
            set
            {
                _commandModel = value;
                OnPropertyChanged();
            }
        }
        List<SatelliteCommandModel> commands = new List<SatelliteCommandModel>();
        public TestWindow()
        {
            InitializeComponent();
            using (var fileStream = File.OpenRead("Cache/method"))
            {
                try
                {
                    commands = MessagePackSerializer.Deserialize<List<SatelliteCommandModel>>(fileStream);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (commands.Count > 0)
            {
                var index = 4;
                CommandModel = commands[index];
                Parameter = commands[index].MethodIn[0];
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
