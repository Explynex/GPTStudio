using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using Telegram.Bot.Types.ReplyMarkups;

namespace GPTStudio.TelegramProvider;

internal enum KeyboardCallbackData : byte
{
    ModesChatMode,
    ModesInsertMode,
    ModesCompleteMode,
    

    MainMenu,
    SettingsMenu,
    SummaryMenu,
    AboutMenu,
    AdminPanelMenu,
    ModesMenu,
    ModeSettingsMenu,

    LanguagesMenu,
    
    SettingsGenMode,

    AdminTotalUsers,
    AdminTotalChats,
    MainMenuStartChat,
    Tokens,
    Temperature,
    TopP,
    FrequencyPenalty,
    PresencePenalty,
    BestOf,


    RegenerateImage,
}

internal static class KeyboardBuilder
{
    public static InlineKeyboardButton BackToMainButton(string locale)
        => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToMainTitle], $"{KeyboardCallbackData.MainMenu}");

    public static InlineKeyboardButton BackToSettingsButton(string locale)
        => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToSettingsTitle], $"{KeyboardCallbackData.SettingsMenu}");

    public static InlineKeyboardButton BackToModesButton(string locale)
    => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToModesTitle], $"{KeyboardCallbackData.ModesMenu}");
    public static InlineKeyboardButton BackToModeSettingsButton(string locale)
=> InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.Back], $"{KeyboardCallbackData.ModeSettingsMenu}");

    public static InlineKeyboardMarkup LanguagesMarkup(string locale) => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🇺🇦", "lang.🇺🇦|uk"),
            InlineKeyboardButton.WithCallbackData("🇬🇧", "lang.🇬🇧|en"),
            InlineKeyboardButton.WithCallbackData("🇷🇺", "lang.🇷🇺|ru"),
        },
/*        new[]
        {
            InlineKeyboardButton.WithCallbackData("🇩🇪", "lang.de"),
            InlineKeyboardButton.WithCallbackData("🇫🇷", "lang.fr"),
            InlineKeyboardButton.WithCallbackData("🇵🇱", "lang.pl"),
        },*/
        new[] { BackToSettingsButton(locale) },
    });

    public static InlineKeyboardMarkup MainMenuMarkup(string locale, bool? admin)
    {
        var culture = Locale.Cultures[locale];
        var markup = new List<InlineKeyboardButton[]>
        {
            new[] { InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuStartChatting], $"{KeyboardCallbackData.MainMenuStartChat}") },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuSettings], $"{KeyboardCallbackData.SettingsMenu}"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuSummary], $"{KeyboardCallbackData.SummaryMenu}"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuAbout], $"{KeyboardCallbackData.AboutMenu}"),
            },
        };
        if (admin == true)
            markup.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuUsers], $"{KeyboardCallbackData.AdminTotalUsers}"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuChats], $"{KeyboardCallbackData.AdminTotalChats}"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuAdminPanal], $"{KeyboardCallbackData.AdminPanelMenu}"),
            });

        return new(markup);
    }

    public static InlineKeyboardMarkup ModesMenuMarkup(GUser user)
    {
        return new(new[]
        {
            new[] 
            {
                InlineKeyboardButton.WithCallbackData("💬 Chat" + (user.SelectedMode == ModelMode.ChatMode ? "   ✅" : ""),$"{KeyboardCallbackData.ModesChatMode}"),
                InlineKeyboardButton.WithCallbackData("📨 Insert" + (user.SelectedMode == ModelMode.InsertMode ? "   ✅" : ""), $"{KeyboardCallbackData.ModesInsertMode}"),
            },
            new[] 
            {
                InlineKeyboardButton.WithCallbackData("🖍 Complete" + (user.SelectedMode == ModelMode.CompleteMode ? "   ✅" : ""), $"{KeyboardCallbackData.ModesCompleteMode}"),
                InlineKeyboardButton.WithCallbackData("🔬 Mode settings", $"{KeyboardCallbackData.ModeSettingsMenu}")
            },
            new[] { BackToSettingsButton(user.LocaleCode) },
        });
    }

    public static  InlineKeyboardMarkup TokensSettingsMarkup(string localeCode) => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("➕1️⃣","tokens.1"),
            InlineKeyboardButton.WithCallbackData("➕1️⃣0️⃣","tokens.10"),
            InlineKeyboardButton.WithCallbackData("➕1️⃣0️⃣0️⃣","tokens.100"),
            InlineKeyboardButton.WithCallbackData("🔼","tokens.3"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("➖1️⃣","tokens.-1"),
            InlineKeyboardButton.WithCallbackData("➖1️⃣0️⃣","tokens.-10"),
            InlineKeyboardButton.WithCallbackData("➖1️⃣0️⃣0️⃣","tokens.-100"),
            InlineKeyboardButton.WithCallbackData("🔽","tokens.-3"),
        },
        new[] { BackToModeSettingsButton(localeCode) },
    });


    public static InlineKeyboardMarkup FloatKeyboardMarkup(string localeCode, string tag) => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("➕0️⃣,0️⃣1️⃣",$"{tag}.0,01"),
            InlineKeyboardButton.WithCallbackData("➕0️⃣,1️⃣",$"{tag}.0,1"),
            InlineKeyboardButton.WithCallbackData("🔼",$"{tag}.3"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("➖0️⃣,0️⃣1️⃣",$"{tag}.-0,01"),
            InlineKeyboardButton.WithCallbackData("➖0️⃣,1️⃣",$"{tag}.-0,1"),
            InlineKeyboardButton.WithCallbackData("🔽",$"{tag}.-3"),
        },
        new[] { BackToModeSettingsButton(localeCode) },
    });

    public static InlineKeyboardMarkup ModeSettingsMarkup(ModelMode mode, string locale)
    {
        var list = new List<InlineKeyboardButton[]>
        {
                new[] 
                {
                    InlineKeyboardButton.WithCallbackData("🎲 Tokens",$"{KeyboardCallbackData.Tokens}"),
                    InlineKeyboardButton.WithCallbackData("💥 Temperature", $"{KeyboardCallbackData.Temperature}"),
                    InlineKeyboardButton.WithCallbackData("✨ Top P", $"{KeyboardCallbackData.TopP}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🫧 Frequency penalty", $"{KeyboardCallbackData.FrequencyPenalty}"),
                    InlineKeyboardButton.WithCallbackData("🫧 Presence penalty", $"{KeyboardCallbackData.PresencePenalty}"),
                },
                new[] { BackToModesButton(locale) }
        };

        if (mode != ModelMode.ChatMode)
            list.Insert(list.Count-1,new[] { InlineKeyboardButton.WithCallbackData("⚜️ Best of ", $"{KeyboardCallbackData.BestOf}") });
        return new(list);
    }


    public static InlineKeyboardMarkup SettingsMenuMarkup(GUser user)
    {
        var culture = Locale.Cultures[user.LocaleCode];
        return new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(culture[Strings.SettingsGenMode] + culture[user.GenFullyMode == true ? Strings.FullyGenModeMsg : Strings.StreamGenModeMsg ],$"{KeyboardCallbackData.SettingsGenMode}") },
            new[] 
            {
                InlineKeyboardButton.WithCallbackData(culture[Strings.SettingsModelsSettings], $"{KeyboardCallbackData.ModesMenu}"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.SettingsLanguage], $"{KeyboardCallbackData.LanguagesMenu}"),
            },
            new[] { BackToMainButton(user.LocaleCode) },
        });
    }



    public static readonly InlineKeyboardMarkup ImageGenerateMarkup = new(new[]
{
        new[] { InlineKeyboardButton.WithCallbackData("🔄  Regenerate", $"{KeyboardCallbackData.RegenerateImage}"),},
    });
}
