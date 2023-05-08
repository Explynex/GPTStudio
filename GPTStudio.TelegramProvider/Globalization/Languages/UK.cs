namespace GPTStudio.TelegramProvider.Globalization.Languages;
internal class UK
{
    private const string MenuTitleTip = "\n🔰\n🔰 Використовуйте кнопки <b>внизу</b> для навігації. ";

    internal Dictionary<Strings, string> Culture = new()
    {
        {Strings.FirstHelloMsg, "👾 Вітаю! Радий, що Ви приєдналися до нас. Я - віртуальний помічник GPTStudio, чим я можу бути корисним, " },
        {Strings.StartChattingMsg,"👾 Я віртуальний помічник GPTStudio. Як я можу вам допомогти?" },
        {Strings.SuccessChangeLang,"🔸 Мова успішно змінена на: " },

        {Strings.MainMenuTitle,$"🔸 <b>Головне меню</b> {MenuTitleTip}" },
        {Strings.MainMenuStartChatting, "💬  Розпочати чат" },
        {Strings.MainMenuSettings, "⚙️  Налаштування" },
        {Strings.MainMenuAbout, "📖  Інформація" },
        {Strings.MainMenuSummary, "📊  Зведення" },
        {Strings.MainMenuUsers, "👥 Користувачі" },
        {Strings.MainMenuChats, "📚 Чати" },
        {Strings.MainMenuAdminPanal, "🖥 Адмін-панель" },

        {Strings.LanguagesMenuTitle,$"🔸 <b>Налаштування мови</b>\n🔰\n🔰 Використовуйте кнопки <b>внизу</b> щоб обрати мову." },

        {Strings.SummaryForMsg,$"📊 Зведення для:" },
        {Strings.SummaryMemberSince,$"🗓 <b>Учасник з:</b>" },
        {Strings.SummaryTokensGen,$"🌀 <b>Токенов згенеровано:</b>" },
        {Strings.SummaryRequests,$"🔁 <b>Всього запитів:</b>" },

        {Strings.SettingsTitle,$"🔸 <b>Налаштування</b> {MenuTitleTip}" },
        {Strings.SettingsGenMode,"🌀 Режим генерації: " },
        {Strings.SettingsLanguage,"🏳 Мова" },
        {Strings.SettingsModelsSettings,"👾 Налаштування моделей" },

        {Strings.BackToSettingsTitle,"⬅️ Назад до налаштувань" },
        {Strings.BackToMainTitle,"⬅️ Назад у головне меню" },
    };
}
