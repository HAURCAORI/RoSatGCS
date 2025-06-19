using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;
using NLog;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
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


        private ObservableCollection<ParameterModel> _parameters = new ObservableCollection<ParameterModel>();
        private SatelliteCommandModel? _commandModel;

        public ObservableCollection<ParameterModel> Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
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

        private static List<ParameterModel> GetType(ParameterModel param)
        {
            var list = new List<ParameterModel>();
            if(param.CommandModel == null || !param.IsUserDefined)
            {
                list.Add(param);
                return list;
            }

            SatelliteFunctionTypeModel? type;
            if (param.CommandModel.AssociatedType.TryGetValue(param.UserDefinedType, out type))
            {
                if (type.Type == SatelliteFunctionTypeModel.ArgumentType.Struct)
                {
                    // Add Header
                    var header = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.Struct, type.File, type.Name, type.Description );
                    header.CommandModel = param.CommandModel;
                    header.ByteSize = param.ByteSize;
                    header.DataType = SatelliteFunctionFileModel.DataType.UserDefined;
                    header.Sequence = param.Sequence;
                    list.Add(header);

                    foreach (var p in type.Parameters)
                    {
                        p.CommandModel = param.CommandModel;
                        p.IsReadOnly = param.IsReadOnly;
                        p.Sequence = param.Sequence;
                        if (p.IsUserDefined)
                        {
                            list.AddRange(GetType(p));
                        }
                        else
                        {
                            list.Add(p);
                        }
                    }
                }
                else if (type.Type == SatelliteFunctionTypeModel.ArgumentType.Enum)
                {
                    list.Add(param);
                }
            }
            else
            {
                list.Add(param);
            }

            return list;
        }

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
                var index = 1;
                CommandModel = commands[index];


                //var param = commands[index].MethodIn[0];
                var sequence = 1;
                foreach (var param in commands[index].MethodIn)
                {
                    param.CommandModel = CommandModel;
                    param.IsReadOnly = false;
                    param.Sequence = sequence++.ToString();

                    foreach (var t in GetType(param))
                    {
                        Parameters.Add(t);
                    }
                }
            }

        }
    }
}
