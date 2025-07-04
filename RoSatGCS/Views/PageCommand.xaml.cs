﻿using AvalonDock;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace RoSatGCS.Views
{
    /// <summary>
    /// PageCommand.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PageCommand : Page
    {
        public static DockingManager DockingManager { get; private set; }
        public PageCommand()
        {
            InitializeComponent();
            DockingManager = dockManager;
            DataContext = App.Current.Services.GetService(typeof(PageCommandViewModel));
        }
    }
}
