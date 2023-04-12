using GPTStudio.Infrastructure;
using GPTStudio.MVVM.View.Windows;
using System.Reflection;
using System.Windows;

namespace GPTStudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static readonly string WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        protected override void OnStartup(StartupEventArgs e)
        {
            Config.Load();
            new MainWindow().Show();
        }

        public static new void Shutdown()
        {
            if (Config.NeedToUpdate)
                Config.Save();

            Application.Current.Shutdown();
        }
    }
}
