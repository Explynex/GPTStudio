namespace GPTStudio.TelegramProvider.Globalization.Languages;
internal class RU
{
    private const string MenuTitleTip = "\n🔰\n🔰 Используйте кнопки <b>внизу</b> для навигации. ";

    internal Dictionary<Strings, string> Culture = new()
    {
        {Strings.FirstHelloMsg, "👾 Здравствуйте! Рад, что Вы присоединились к нам. Я - виртуальный ассистент GPTStudio, чем я могу быть полезен, " },
        {Strings.StartChattingMsg,"👾 Я виртуальный помощник GPTStudio. Чем я могу вам помочь?" },
        {Strings.SuccessChangeLang,"🔸 Язык успешно изменён на: " },
        {Strings.StopGenerationMsg,"▫️ Остановить генерацию" },
        {Strings.ResponseGenMsg,"⏳ Генерация ответа. . ." },
        {Strings.StreamGenModeMsg,"Потоком" },
        {Strings.FullyGenModeMsg,"Целиком" },
        {Strings.ErrorWhileGenMsg,"❌ Во время генерации произошла ошибка" },

        {Strings.MainMenuTitle,$"🔸 <b>Главное меню</b> {MenuTitleTip}" },
        {Strings.MainMenuStartChatting, "💬  Начать чат" },
        {Strings.MainMenuSettings, "⚙️  Настройки" },
        {Strings.MainMenuAbout, "📖  Информация" },
        {Strings.MainMenuSummary, "📊  Сводка" },
        {Strings.MainMenuUsers, "👥 Пользователи" },
        {Strings.MainMenuChats, "📚 Чаты" },
        {Strings.MainMenuAdminPanal, "🖥 Админ-панель" },

        {Strings.LanguagesMenuTitle,$"🔸 <b>Настройки языка</b>\n🔰\n🔰 Используйте кнопки <b>внизу</b> чтобы выбрать язык." },
        {Strings.ModesMenuTitle,$"🔸 <b>Режимы работы</b>\n🔰\n🔰 Используйте кнопки <b>внизу</b> чтобы выбрать или настроить режим." },

        {Strings.SummaryForMsg,$"📊 Сводка для:" },
        {Strings.SummaryMemberSince,$"🗓 <b>Участник с:</b>" },
        {Strings.SummaryTokensGen,$"🌀 <b>Токенов сгенерировано:</b>" },
        {Strings.SummaryRequests,$"🔁 <b>Всего запросов:</b>" },

        {Strings.SettingsTitle,$"🔸 <b>Настройки</b> {MenuTitleTip}" },
        {Strings.SettingsGenMode,"🌀 Режим генерации: " },
        {Strings.SettingsLanguage,"🏳 Язык" },
        {Strings.SettingsModelsSettings,"👾 Режимы" },

        {Strings.BackToSettingsTitle,"⬅️ Назад к настройкам" },
        {Strings.BackToMainTitle,"⬅️ Назад в главное меню" },
        {Strings.BackToModesTitle,"⬅️ Назад к режимам" },
        {Strings.Back,"⬅️ Назад" },
    };
}
