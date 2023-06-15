namespace GPTStudio.TelegramProvider.Globalization.Languages;
internal class EN
{
    private const string MenuTitleTip = "\n🔰\n🔰 Use the buttons <b>below</b> to navigate. ";

    internal Dictionary<Strings, string> Culture = new()
    {
        {Strings.FirstHelloMsg, "👾 Hello! Glad you have joined us. I am a virtual assistant of GPTStudio, how can I help you, " },
        {Strings.StartChattingMsg,"👾 I am virtual assistant GPTStudio. How can I assist you?" },
        {Strings.SuccessChangeLang,"🔸 Language successfully changed to: " },
        {Strings.StopGenerationMsg,"▫️ Stop generation" },
        {Strings.ResponseGenMsg,"⏳ Generating response. . ." },
        {Strings.StreamGenModeMsg,"Stream" },
        {Strings.FullyGenModeMsg,"Fully" },
        {Strings.RequestErrorMsg,"❌ An error occurred while processing the request" },

        {Strings.MainMenuTitle,$"🔸 <b>Main menu</b> {MenuTitleTip}" },
        {Strings.MainMenuStartChatting, "💬  Start chatting" },
        {Strings.MainMenuSettings, "⚙️  Settings" },
        {Strings.MainMenuAbout, "📖  About" },
        {Strings.MainMenuSummary, "📊  Summary" },
        {Strings.MainMenuAdminPanel, "🖥 Admin panel" },

        {Strings.LanguagesMenuTitle,$"🔸 <b>Language settings</b>\n🔰\n🔰 Use the buttons <b>below</b> to select language." },
        {Strings.ModesMenuTitle,$"🔸 <b>Operating modes</b>\n🔰\n🔰 Use the buttons <b>below</b> to select or configure mode." },
        {Strings.AdminPanelTitle,$"🔸 <b>Administrator menu</b> {MenuTitleTip}" },
        {Strings.ServicesMenuTitle,$"🔸 <b>Services menu</b> {MenuTitleTip}" },

        {Strings.SummaryForMsg,$"📊 Summary for:" },
        {Strings.SummaryMemberSince,$"🗓 <b>Member since:</b>" },
        {Strings.SummaryTokensGen,$"🌀 <b>Tokens generated:</b>" },
        {Strings.SummaryRequests,$"🔁 <b>Total requests:</b>" },

        {Strings.SettingsTitle,$"🔸 <b>Settings</b> {MenuTitleTip}" },
        {Strings.SettingsGenMode,"🌀 Generation mode: " },
        {Strings.SettingsLanguage,"🏳 Language" },
        {Strings.SettingsModelsSettings,"👾 Modes" },

        {Strings.BackToSettingsTitle,"⬅️ Back to settings" },
        {Strings.BackToMainTitle,"⬅️ Back to main menu" },
        {Strings.BackToModesTitle,"⬅️ Back to modes menu" },
        {Strings.Back,"⬅️ Back" },
    };
}
