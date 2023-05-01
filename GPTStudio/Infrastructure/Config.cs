using GPTStudio.Infrastructure.Models;
using LanguageDetection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GPTStudio.Infrastructure;


internal static class Config
{
    public static Models.Properties Properties { get; set; }
    public static SpeecherInfo[] AviableVoices { get; set; }
    public static Dictionary<string, LanguageInfo> LanguagesConfig { get; private set; }

    public static LanguageDetector LangDetector { get; set; }

    private static readonly string Path = App.WorkingDirectory + "\\properties.json";
    public static bool NeedToUpdate { get; set; }

    static Config()
    {
        if (File.Exists(App.UserdataDirectory + "LangConfig"))
        {
            Config.LanguagesConfig = JsonSerializer.Deserialize<Dictionary<string, LanguageInfo>>(File.ReadAllText(App.UserdataDirectory + "LangConfig"));
            UpdateLangDetector();
        }
        else
        {
            LangDetector = new();
            InitDefaultLanguages();
        }
    }

    public static void UpdateLangDetector()
    {
        Config.LangDetector = new();
        var selectedLang = Config.LanguagesConfig.Where(o => o.Value.Selected)?.Select(o => o.Key).ToArray();
        if (selectedLang?.Length > 0)
            LangDetector.AddLanguages(selectedLang);
    }

    public static bool Load()
    {
        if (!File.Exists(Path))
        {
            Properties = new();
            NeedToUpdate = true;
            return true;
        }

        try
        {
            Properties = JsonSerializer.Deserialize<Models.Properties>(File.ReadAllText(Path));
            return true;
        }
        catch
        {
            File.Delete(Path);
            Properties = new();
            NeedToUpdate = true;
            return false;
        }
    }

    public static void Save()
    {
        if (Properties == null)
            return;

        File.WriteAllText(Path, JsonSerializer.Serialize<Models.Properties>(Properties));
    }


    private static void InitDefaultLanguages()
    {
        Config.LanguagesConfig = new()
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
}

