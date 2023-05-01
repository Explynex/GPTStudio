using GPTStudio.Infrastructure;
using GPTStudio.Infrastructure.Models;
using GPTStudio.MVVM.View.Windows;
using GPTStudio.MVVM.ViewModels;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace GPTStudio;

public partial class App : Application
{
    internal static readonly string WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    internal static readonly string UserdataDirectory = $"{WorkingDirectory}\\.userdata\\";
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (sender, arg) =>
        {
            MessageBox.Show(arg.Exception.ToString());
            Shutdown();
        };

        if (!Directory.Exists(UserdataDirectory))
        {
            DirectoryInfo dir = Directory.CreateDirectory(UserdataDirectory);
            dir.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

        Config.Load();
        new MainWindow().Show();
    }

    public static new void Shutdown()
    {
        if (Config.NeedToUpdate)
            Config.Save();

        if (SettingsViewModel.NeedLanguagesConfigUpdate)
            File.WriteAllText(UserdataDirectory+ "LangConfig", JsonSerializer.Serialize(Config.LanguagesConfig));

        if (Chat.NeedToUpdate)
            Chat.Save();

        Application.Current.Shutdown();
    }
}
