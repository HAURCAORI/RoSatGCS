using AdonisUI.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
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
using RoSatGCS.Utils.Localization;

namespace RoSatGCS.Views
{
    /// <summary>
    /// DialogueFileSearch.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WindowFileSearch : AdonisWindow, INotifyPropertyChanged
    {
        private string? path;
        private readonly List<string> allowedExtensions;
        private readonly string filterDescription;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Path { get =>  path ?? ""; set { path = value; NotifyPropertyChanged(nameof(Path)); } }

        public ICommand Ok { get; private set; }
        public ICommand Cancel { get; private set; }
        public ICommand OpenDialogue { get; private set; }

        public WindowFileSearch(IEnumerable<string> extensions, string filterDescription = "Supported files")
        {
            InitializeComponent();
            DataContext = this;

            allowedExtensions = extensions
            .Select(ext => ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower())
            .ToList();

            this.filterDescription = filterDescription;

            Ok = new RelayCommand(OkButton_Click);
            Cancel = new RelayCommand(CancelButton_Click);
            OpenDialogue = new RelayCommand(OpenButton_Click);
        }

        public void OkButton_Click()
        {
            Path = tbPath.Text;
            if(!File.Exists(Path))
            {
                System.Windows.MessageBox.Show(TranslationSource.Instance["zNoSuchFile"], TranslationSource.Instance["zFileNotFound"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            string ext = System.IO.Path.GetExtension(Path)?.ToLower() ?? "";

            if (allowedExtensions.FirstOrDefault() != ".*" && !allowedExtensions.Contains(ext))
            {
                if (allowedExtensions.FirstOrDefault() == ".json")
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zExtensionJson"], TranslationSource.Instance["zExtensionMismatch"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zExtensionMismatch"] + ":\r\n" + string.Join(", ", allowedExtensions), TranslationSource.Instance["zExtensionMismatch"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                return;
            }

            DialogResult = !string.IsNullOrEmpty(Path);

            Close();
        }

        public void CancelButton_Click()
        {
            Path = "";
            DialogResult = false;
            Close();
        }

        public void OpenButton_Click()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "File",
                Filter = BuildFilterString()
            };

            bool? result = dialog.ShowDialog();

            if(result == true)
            {
                Path = dialog.FileName;
            }
        }
        private string BuildFilterString()
        {
            string extFilter = string.Join(";", allowedExtensions.Select(ext => "*" + ext));
            return $"{filterDescription} ({extFilter})|{extFilter}";
        }

        internal void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
