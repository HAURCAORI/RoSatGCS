﻿using AdonisUI.Controls;
using NLog;
using RoSatGCS.Utils.Localization;
using RoSatGCS.ViewModels;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoSatGCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class Parser
    {
        [LibraryImport("FIDLParser.dll")]
        public static partial IntPtr Create();
        [LibraryImport("FIDLParser.dll")]
        public static partial void Destroy(IntPtr instance);
        [LibraryImport("FIDLParser.dll")]
        public static partial void Dispose(IntPtr instance);
        [LibraryImport("FIDLParser.dll")]
        public static partial IntPtr getExtension(IntPtr instance);
    }

    public partial class MainWindow : AdonisWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(MainWindowViewModel));



            // 아래 테스트용 코드들
            Logger.Debug("Test");
             //TranslationSource.SetLanguage("ko-KR");

             var p =  Parser.Create();
            System.Diagnostics.Debug.WriteLine("Test");
            IntPtr ret = Parser.getExtension(p);
            var str = Marshal.PtrToStringUTF8(ret);
            Parser.Dispose(ret);
            System.Diagnostics.Debug.WriteLine(str);
            
            Parser.Destroy(p);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SerialStatus.Status = GCSTypes.IndicatorStatus.Error;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SerialStatus.Status = GCSTypes.IndicatorStatus.Ok;

        }
    }
}