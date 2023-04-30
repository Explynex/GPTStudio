using GPTStudio.Infrastructure;
using GPTStudio.Infrastructure.Azure;
using GPTStudio.MVVM.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Data;

namespace GPTStudio.MVVM.ViewModels;

internal class SettingsViewModel : ObservableObject
{
    public static bool NeedLanguagesConfigUpdate { get; private set; }
    public SpeecherInfo[] VoicesList { get; private set; }

    public RelayCommand SelectLanguageCommand { get; set; }
    public RelayCommand LoadVoicesCommand { get; set; }
    public RelayCommand ConfigureVoiceCommand { get; set; }
    public static Infrastructure.Models.Properties Properties => Config.Properties;

    private ListCollectionView voicesFilter;

    internal class LanguageInfo
    {
        public string CountryName { get; }
        public string SelectedSpeecher { get; set; }
        public bool Selected { get; set; }
        public LanguageInfo(string countryName)
        {
            CountryName = countryName;
        }

        public override string ToString() => CountryName;
    }

    private KeyValuePair<string, LanguageInfo>? _selectedLang;
    public KeyValuePair<string,LanguageInfo>? SelectedLang
    {
        get => _selectedLang;
        set => SetProperty(ref _selectedLang, value);
    }


    private int _voiceIndex = -1;
    public int VoiceIndex
    {
        get => _voiceIndex;
        set
        {
            if (value != -1)
            {
                SelectedLang.Value.Value.SelectedSpeecher = (voicesFilter.GetItemAt(value) as SpeecherInfo).ShortName;
                NeedLanguagesConfigUpdate = true;
            }
                
            _voiceIndex = value;
        }
    }

    public Dictionary<string,LanguageInfo> Languages { get; set; }

    public bool UsingMarkdown
    {
        get => Config.Properties.UsingMarkdown;
        set
        {
            (MainWindowViewModel.MessengerV.DataContext as MessengerViewModel).UsingMarkdown = Config.Properties.UsingMarkdown = value;
        }
    }

    private bool VoiceFilter(object o)
    {
        var sender = o as SpeecherInfo;
        if (SelectedLang == null)
            return false;

        if (sender.ShortName.StartsWith(SelectedLang.Value.Key))
            return true;

        return false;
    }
    private void InitDefaultLanguages()
    {
        Languages = new()
    {
        { "af", new("Afrikaans") },
        { "ar",new("Arabic")},
        { "bg",new("Bulgarian")},
        { "bn",new("Bengali")},
        { "cs",new("Czech")},
        { "da", new("Danish") },
        { "de", new("German") },
        { "el", new("Greek") },
        { "en", new("English") },
        { "es", new("Spanish") },
        { "et", new("Estonian") },
        { "fa", new("Persian") },
        { "fi", new("Finnish") },
        { "fr", new("French") },
        { "gu", new("Gujarati") },
        { "he", new("Hebrew") },
        { "hi", new("Hindi") },
        { "hr", new("Croatian") },
        { "hu", new("Hungarian") },
        { "id", new("Indonesian") },
        { "it", new("Italian") },
        { "ja", new("Japanese") },
        { "kn", new("Kannada") },
        { "ko", new("Korean") },
        { "lt", new("Lithuanian") },
        { "lv", new("Latvian") },
        { "mk", new("Macedonian") },
        { "ml", new("Malayalam") },
        { "mr", new("Marathi") },
        { "ne", new("Nepali") },
        { "nl", new("Dutch") },
        { "no", new("Norwegian") },
        { "pl", new("Polish") },
        { "pt", new("Portuguese") },
        { "ro", new("Romanian") },
        { "ru", new("Russian") },
        { "sk", new("Slovak") },
        { "sl", new("Slovenian") },
        { "so", new("Somali") },
        { "sq", new("Albanian") },
        { "sv", new("Swedish") },
        { "sw", new("Swahili") },
        { "ta", new("Tamil") },
        { "te", new("Telugu") },
        { "th", new("Thai") },
        { "tr", new("Turkish") },
        { "uk", new("Ukrainian") },
        { "ur", new("Urdu") },
        { "vi", new("Vietnamese") },
        { "zh-chs", new("Chinese") },
    };
    }

    public SettingsViewModel()
    {
        if (File.Exists(App.UserdataDirectory + "LangConfig"))
            Languages = JsonSerializer.Deserialize<Dictionary<string, LanguageInfo>>(File.ReadAllText(App.UserdataDirectory + "LangConfig"));
        else
            InitDefaultLanguages();

        SelectLanguageCommand = new(o =>
        {
            var info = ((KeyValuePair<string, LanguageInfo>)o).Value;
            info.Selected = !info.Selected;
            if(info.Selected)
            {
                ConfigureVoiceCommand.Execute(o);
            }
        });

        LoadVoicesCommand = new(o =>
        {
            if(VoicesList == null && File.Exists(App.UserdataDirectory + "voices"))
            {
                VoicesList = JsonSerializer.Deserialize<SpeecherInfo[]>(File.ReadAllText(App.UserdataDirectory + "voices"));
                voicesFilter = (ListCollectionView)CollectionViewSource.GetDefaultView(VoicesList);
                voicesFilter.Filter += VoiceFilter;
                OnPropertyChanged(nameof(VoicesList));
            }
        });

        ConfigureVoiceCommand = new(o =>
        {
            SelectedLang = (KeyValuePair<string,LanguageInfo>)o;
            voicesFilter.Refresh();

            if (SelectedLang.Value.Value.SelectedSpeecher == null) 
                VoiceIndex = 0 ;
            else
            {
                for (int i = 0; i < voicesFilter.Count; i++)
                {
                    var item = (voicesFilter.GetItemAt(i) as SpeecherInfo);
                    if (item.ShortName.GetHashCode() == SelectedLang.Value.Value.SelectedSpeecher.GetHashCode())
                    {
                        _voiceIndex = i;
                        break;
                    }
                }
            }

            OnPropertyChanged(nameof(VoiceIndex));
        });
    }
}
