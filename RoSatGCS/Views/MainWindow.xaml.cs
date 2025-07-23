using AdonisUI.Controls;
using NLog;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Satellites;
using RoSatGCS.Utils.Satellites.Core;
using RoSatGCS.Utils.Satellites.TLE;
using RoSatGCS.Utils.Satellites.Observation;
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
    
    /*
    public partial class Parser
    {
        [LibraryImport("RoSatParser.dll")]
        public static partial IntPtr Create();
        [LibraryImport("RoSatParser.dll")]
        public static partial void Destroy(IntPtr instance);
        [LibraryImport("RoSatParser.dll")]
        public static partial void Dispose(IntPtr instance);
        [LibraryImport("RoSatParser.dll")]
        public static partial IntPtr getExtension(IntPtr instance);
    }*/

    public partial class MainWindow : AdonisWindow
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(MainWindowViewModel));

            
            // 아래 테스트용 코드들
            string tle = "ISS (ZARYA)\r\n1 25544U 98067A   24328.29153615  .00029143  00000-0  51837-3 0  9997\r\n2 25544  51.6408 247.1413 0007321 258.8281 244.5643 15.49886778483225";
            var satellite = new Satellite(new TLE(tle),"ISS");

            var eciTime =  satellite.PositionEci(DateTime.Now);
            var geoTime =  new GeoCoordinate(eciTime);
            Logger.Trace(geoTime.ToString());
            //TranslationSource.SetLanguage("ko-KR");
            

            /*
            var p =  Parser.Create();
            System.Diagnostics.Debug.WriteLine("Test");
            IntPtr ret = Parser.getExtension(p);
            var str = Marshal.PtrToStringUTF8(ret);
            Parser.Dispose(ret);
            System.Diagnostics.Debug.WriteLine(str);
            
            Parser.Destroy(p);*/
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