using AvalonDock;
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
using System.Windows.Shapes;

namespace RoSatGCS.Views
{
    /// <summary>
    /// WindowOnboardScheduler.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WindowOnboardScheduler : Window
    {
        
        public WindowOnboardScheduler()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(WindowOnboardSchedulerViewModel));
            
            var dc = DataContext as WindowOnboardSchedulerViewModel;
            if (dc == null) { return; }

            dc.CloseAction ??= new Action(() => this.Close());
            dc.CloseHexEditor ??= new Action(CloseHexEditor);
            dc.OpenHexEditor ??= new Action<string>(OpenHexEditor);
            dc.HighlightHexEditor ??= new Action<int, int>(HighlightHexEditor);
            dc.SaveHexEditor ??= new Action(SaveHexEditor);
        }

        public void CloseHexEditor()
        {
            HexEditor.CloseProvider(true);
        }

        public void OpenHexEditor(string path)
        {
            HexEditor.FileName = null;
            HexEditor.FileName = path;
        }

        public void HighlightHexEditor(int n, int m)
        {
            HexEditor.AddHighLight(n, m);
        }

        private void SaveHexEditor()
        {
            HexEditor.SubmitChanges();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseHexEditor();
        }
    }
}
