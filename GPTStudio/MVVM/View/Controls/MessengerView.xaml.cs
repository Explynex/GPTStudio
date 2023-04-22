using GPTStudio.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GPTStudio.MVVM.View.Controls
{
    /// <summary>
    /// Логика взаимодействия для MessengerView.xaml
    /// </summary>
    public partial class MessengerView : UserControl
    {
        private MessengerViewModel viewModel;
        public MessengerView()
        {
            InitializeComponent();
            viewModel = new MessengerViewModel();
            DataContext = viewModel;
            MessengerViewModel.ChatScrollViewer = Utils.Presentation.GetDescendantByType(messages, typeof(ScrollViewer)) as ScrollViewer;
        }
        
        
        private void Border_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var IsAlreadySelected = messages.SelectedIndex != -1;
            if (!IsAlreadySelected && e.ClickCount != 2)
            {
                e.Handled = true;
                return;
            }

            var s = (sender as Grid).TemplatedParent as ListBoxItem;
            s.IsSelected = !s.IsSelected;
            e.Handled = true;
        }

        private void PreviewCancelRightMouseButton(object sender, System.Windows.Input.MouseButtonEventArgs e) => e.Handled = true;

        private void UnselectAllMessages(object sender, RoutedEventArgs e) => messages.UnselectAll();
    }

}
