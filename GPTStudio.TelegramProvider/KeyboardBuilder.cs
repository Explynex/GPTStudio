using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace GPTStudio.TelegramProvider;
internal static class KeyboardBuilder
{
    public static readonly InlineKeyboardButton BackButton = InlineKeyboardButton.WithCallbackData("⬅️  Back", "back1");

    public static readonly InlineKeyboardMarkup MainMenuMarkup = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("💬  Start chatting","1.1"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⚙️  Settings","1.2"),
            InlineKeyboardButton.WithCallbackData("📊  Summary","1.3"),
            InlineKeyboardButton.WithCallbackData("📖  About","1.4"),
        },
    });

    public static readonly InlineKeyboardMarkup MainAdminMenuMarkup = new(new[]
{
        new[]
        {
            InlineKeyboardButton.WithCallbackData("💬  Start chatting","1.1"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("⚙️  Settings","1.2"),
            InlineKeyboardButton.WithCallbackData("📊  Summary","1.3"),
            InlineKeyboardButton.WithCallbackData("📖  About","1.4"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("👥 Users","1.5"),
            InlineKeyboardButton.WithCallbackData("📚 Chats","1.6"),
            InlineKeyboardButton.WithCallbackData("🖥 Admin panel","1.7"),
        },
    });

    public static readonly InlineKeyboardMarkup SettingsMenuMarkup = new(new[]
{
        new[] { InlineKeyboardButton.WithCallbackData("🛠  System message","2.2")},
        new[] { InlineKeyboardButton.WithCallbackData("📜  Generation mode","2.3")},
        new[] { InlineKeyboardButton.WithCallbackData("🏳  Language", "2.4")},
        new[] { InlineKeyboardButton.WithCallbackData("🏳  Language", "2.4")},
        new[] { BackButton },

    });

    public static readonly InlineKeyboardMarkup ImageGenerateMarkup = new(new[]
{
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄  Regenerate","img.1"),
        },
    });
}
