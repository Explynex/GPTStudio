using GPTStudio.Infrastructure;
using GPTStudio.Infrastructure.Models;
using GPTStudio.MVVM.Core;
using GPTStudio.MVVM.View.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Data;

namespace GPTStudio.MVVM.ViewModels;

internal class SettingsViewModel : ObservableObject
{
    public static bool NeedLanguagesConfigUpdate { get; private set; }

    public RelayCommand SelectLanguageCommand { get; set; }
    public RelayCommand LoadVoicesCommand { get; set; }
    public RelayCommand ConfigureVoiceCommand { get; set; }


    public static Infrastructure.Models.Properties Properties => Config.Properties;
    public SpeecherInfo[] VoicesList                          => Config.AviableVoices;
    public Dictionary<string, LanguageInfo> LanguagesList     => Config.LanguagesConfig;

    private ListCollectionView voicesFilter;

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

    public bool UsingMarkdown
    {
        get => Config.Properties.UsingMarkdown;
        set => (MainWindowViewModel.MessengerV.DataContext as MessengerViewModel).UsingMarkdown = Config.Properties.UsingMarkdown = value;
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


    public SettingsViewModel()
    {

        SelectLanguageCommand = new(o =>
        {
            var info = ((KeyValuePair<string, LanguageInfo>)o).Value;
            info.Selected = !info.Selected;
            if(info.Selected)
            {
                ConfigureVoiceCommand.Execute(o);
            }
            NeedLanguagesConfigUpdate = true;

            ModenPopup.ClosingAction = Config.UpdateLangDetector;
        });

        LoadVoicesCommand = new(o =>
        {
            if(Config.AviableVoices == null && File.Exists(App.UserdataDirectory + "voices"))
            {
                Config.AviableVoices = JsonSerializer.Deserialize<SpeecherInfo[]>(File.ReadAllText(App.UserdataDirectory + "voices"));
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
