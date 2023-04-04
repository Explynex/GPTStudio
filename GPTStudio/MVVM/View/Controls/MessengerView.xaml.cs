using GPTStudio.MVVM.ViewModels;
using System.Windows.Controls;

namespace GPTStudio.MVVM.View.Controls
{
    /// <summary>
    /// Логика взаимодействия для MessengerView.xaml
    /// </summary>
    public partial class MessengerView : UserControl
    {
        public MessengerView()
        {
            InitializeComponent();
            DataContext = new MessengerViewModel();
        }

    }
}
