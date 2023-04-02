using GPTStudio.MVVM.View.Windows;
using System.Windows;

namespace GPTStudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow().Show();
        }

        public static new void Shutdown()
        {
            Application.Current.Shutdown();
        }
    }
}
