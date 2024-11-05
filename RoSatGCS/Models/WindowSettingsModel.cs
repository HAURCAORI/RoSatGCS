using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RoSatGCS.Models
{
    public class WindowSettingsModel : ObservableObject
    {

        public WindowSettingsModel() 
        {
            languageList = new List<LanguageOption>();
            foreach (LanguageOption val in Enum.GetValues(typeof(LanguageOption)))
            {
                languageList.Add(val);
            }
        }

        #region enums
        [Flags]
        public enum OptionChanged : ulong
        {
            General_Interface_Language = 1 << 0,
            General_Interface_Theme = 1 << 1,
        }

        public enum LanguageOption
        {
            Korean,
            English
        }
        #endregion

        #region fields
        private static OptionChanged optionChanged;
        private LanguageOption language;
        private int languageIndex;
        private List<LanguageOption> languageList;
        #endregion

        #region properties
        public LanguageOption Language
        {
            get { return language; }
            set {  SetProperty(ref language, value); Changed(OptionChanged.General_Interface_Language); }
        }
        public int LanguageIndex
        {
            get { return languageIndex; }
            set { SetProperty(ref languageIndex, value);
                if (Enum.IsDefined(typeof(LanguageOption), value))
                    Language = (LanguageOption)value;
            }
        }

        public List<LanguageOption> LanguageList
        {
            get { return languageList; }
        }
        #endregion

        public static void Changed(OptionChanged op)
        {
            optionChanged |= op;
        }

        public void UpdateList()
        {
            var index = LanguageIndex;
            OnPropertyChanged(nameof(LanguageList));
            LanguageIndex = index;
        }

    }
}
