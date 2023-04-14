using GPTStudio.Infrastructure;
using GPTStudio.MVVM.View.Windows;
using System.IO;
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
        internal static readonly string UserdataDirectory = $"{WorkingDirectory}\\.userdata\\";
        protected override void OnStartup(StartupEventArgs e)
        {
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


            Application.Current.Shutdown();
        }
    }
}
