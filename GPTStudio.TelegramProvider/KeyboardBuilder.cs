using GPTStudio.TelegramProvider.Globalization;
using Telegram.Bot.Types.ReplyMarkups;

namespace GPTStudio.TelegramProvider;
internal static class KeyboardBuilder
{
    public static InlineKeyboardButton BackToMainButton(string locale)
        => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToMainTitle], "back1");

    public static InlineKeyboardButton BackToSettingsButton(string locale)
        => InlineKeyboardButton.WithCallbackData(Locale.Cultures[locale][Strings.BackToSettingsTitle], "back2");

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
            new[] { InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuStartChatting], "1.1") },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuSettings], "1.2"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuSummary], "1.3"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuAbout], "1.4"),
            },
        };
        if (admin == true)
            markup.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuUsers], "1.5"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuChats], "1.6"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.MainMenuAdminPanal], "1.7"),
            });

        return new(markup);
    }


    public static InlineKeyboardMarkup SettingsMenuMarkup(string locale)
    {
        var culture = Locale.Cultures[locale];
        return new(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData(culture[Strings.SettingsGenMode],"2.1") },
            new[] 
            {
                InlineKeyboardButton.WithCallbackData(culture[Strings.SettingsModelsSettings], "2.2"),
                InlineKeyboardButton.WithCallbackData(culture[Strings.SettingsLanguage], "2.3"),
            },
            new[] { BackToMainButton(locale) },
        });
    }



    public static readonly InlineKeyboardMarkup ImageGenerateMarkup = new(new[]
{
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄  Regenerate","img.1"),
        },
    });
}
