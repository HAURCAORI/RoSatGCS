using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class WindowSettingsViewModel : ViewModelBase
    {
        private WindowSettingsModel settingsModel;
        public ICommand ChangeLanguage { get; set; }
        public WindowSettingsViewModel()
        {
            settingsModel = new WindowSettingsModel();
            ChangeLanguage = new RelayCommand(OnChangeLanguage);
        }
        
        private void OnChangeLanguage()
        {
            switch(settingsModel.Language)
            {
                case WindowSettingsModel.LanguageOption.Korean:
                    TranslationSource.SetLanguage("ko-KR");
                    break;
                case WindowSettingsModel.LanguageOption.English:
                    TranslationSource.SetLanguage("en-US");
                    break;
                default:
                    break;
            }
            settingsModel.UpdateList();
        }

        public WindowSettingsModel SettingsModel
        {
            get { return settingsModel; }
        }
    }
}
