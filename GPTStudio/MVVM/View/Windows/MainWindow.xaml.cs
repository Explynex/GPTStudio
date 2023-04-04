using GPTStudio.MVVM.ViewModels;
using System.Windows;

namespace GPTStudio.MVVM.View.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void WindowDragMove(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(WindowState == WindowState.Maximized)
            {
                WindowState      = WindowState.Normal;
                var position     = Utils.Win32.GetMousePosition();
                Left             = position.X - this.Width / 2;
                Top              = position.Y - 30;
            }

            DragMove();
        }

        private void WindowShutdown(object sender, RoutedEventArgs e) => App.Shutdown();
        private void WindowMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void WindowMaximize(object sender, RoutedEventArgs e)
        {
            if(WindowState == WindowState.Normal)
            {
                ResizeMode  = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            }
            else
            {
                ResizeMode  = ResizeMode.CanResize;
                WindowState = WindowState.Normal;
            }
                
        }

    }
}
