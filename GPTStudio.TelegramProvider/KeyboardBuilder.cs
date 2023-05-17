using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace GPTStudio.TelegramProvider;

internal enum KeyboardCallbackData : byte
{
    ModesChatMode,
    ModesEditMode,
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

    RegenerateImage,
}

internal static class KeyboardBuilder
{
    public static InlineKeyboardButton BackToMainButton(string locale)
        => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToMainTitle], $"{KeyboardCallbackData.MainMenu}");

    public static InlineKeyboardButton BackToSettingsButton(string locale)
        => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToSettingsTitle], $"{KeyboardCallbackData.SettingsMenu}");

    public static InlineKeyboardMarkup LanguagesMarkup(string locale) => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🇺🇦", "lang.🇺🇦.uk"),
            InlineKeyboardButton.WithCallbackData("🇬🇧", "lang.🇬🇧.en"),
            InlineKeyboardButton.WithCallbackData("🇷🇺", "lang.🇷🇺.ru"),
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

    public static InlineKeyboardMarkup ModelsSettingsMarkup(string locale,GUser user)
    {
        return new(new[]
        {
            new[] 
            {
                InlineKeyboardButton.WithCallbackData("💬 Chat" + (user.Mode == ModelMode.Chat ? "   ✅" : ""),$"{KeyboardCallbackData.ModesChatMode}"),
                InlineKeyboardButton.WithCallbackData("✂️ Edit" +(user.Mode == ModelMode.Edit ? "   ✅" : ""), $"{KeyboardCallbackData.ModesEditMode}")
            },
            new[] 
            {
                InlineKeyboardButton.WithCallbackData("📨 Insert" + (user.Mode == ModelMode.Insert ? "   ✅" : ""), $"{KeyboardCallbackData.ModesInsertMode}"),
                InlineKeyboardButton.WithCallbackData("🖍 Complete" + (user.Mode == ModelMode.Complete ? "   ✅" : ""), $"{KeyboardCallbackData.ModesCompleteMode}")
            },
            new[] { InlineKeyboardButton.WithCallbackData("🔬 Mode settings", $"{KeyboardCallbackData.ModeSettingsMenu}") },
            new[] { BackToSettingsButton(locale) },
        });
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
