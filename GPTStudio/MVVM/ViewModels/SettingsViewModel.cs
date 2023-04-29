using GPTStudio.Infrastructure;
using GPTStudio.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GPTStudio.MVVM.ViewModels;

internal class SettingsViewModel : ObservableObject
{
    public static Infrastructure.Models.Properties Properties => Config.Properties;

    internal struct LanguageInfo
    {
        public string CountryName { get; }
        public string ISO { get; }
        public bool Selected { get; set; }
        public LanguageInfo(string countryName,string code)
        {
            CountryName = countryName;
            ISO = code;
        }
    }

    public LanguageInfo[] Languages { get; set; } = new LanguageInfo[]
    {
        new("Arabic ","ar-EG"),
        new("Bulgarian","bg-BG"),
        new("Catalan","ca-ES"),
        new("Czech","cs-CZ"),
        new("Danish","da-DK")
    };

    /*LanguageData*/
    public bool UsingMarkdown
    {
        get => Config.Properties.UsingMarkdown;
        set
        {
            (MainWindowViewModel.MessengerV.DataContext as MessengerViewModel).UsingMarkdown = Config.Properties.UsingMarkdown = value;
        }
    }
}
