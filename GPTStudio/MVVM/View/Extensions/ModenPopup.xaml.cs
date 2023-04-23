using GPTStudio.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GPTStudio.MVVM.View.Extensions
{
    public partial class ModenPopup : Grid
    {
        public static readonly DependencyProperty IsOpenProperty =
           DependencyProperty.Register("IsOpen", typeof(bool), typeof(ModenPopup), new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));

        public static readonly DependencyProperty PopupContentProperty =
          DependencyProperty.Register("PopupContent", typeof(object), typeof(ModenPopup), new PropertyMetadata(null));

        private readonly DoubleAnimation OpacityAnimation;
        private readonly ThicknessAnimation MarginOpenAnimation;
        private readonly ThicknessAnimation MarginCloseAnimation;
        private readonly Duration AnimationsDuration    = new(TimeSpan.FromSeconds(0.4));
        private readonly IEasingFunction EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut };
        private bool locked = false;

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set
            {
                SetValue(IsOpenProperty, value);
                if (value)
                {
                    Visibility = Visibility.Visible;
                    OpacityAnimation.From = 0;
                    OpacityAnimation.To = 1;
                    PopupPresenter.BeginAnimation(ContentPresenter.MarginProperty, MarginOpenAnimation);
                    this.BeginAnimation(UserControl.OpacityProperty, OpacityAnimation);
                }
                else
                {
                    OpacityAnimation.From = 1;
                    OpacityAnimation.To = 0;
                    PopupPresenter.BeginAnimation(ContentPresenter.MarginProperty,MarginCloseAnimation);
                    this.BeginAnimation(UserControl.OpacityProperty, OpacityAnimation);
                }

            }
        }

        public object PopupContent
        {
            get => GetValue(PopupContentProperty);
            set => SetValue(PopupContentProperty, value);
        }

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var popup = sender as ModenPopup;

            if(e.NewValue != null)
            {
                popup.IsOpen = (bool)e.NewValue;
            }
        }

        public ModenPopup()
        {
            InitializeComponent();

            OpacityAnimation     = new(1, 0, new Duration(TimeSpan.FromSeconds(0.4)));
            MarginOpenAnimation  = new(new Thickness(0), AnimationsDuration) { EasingFunction = EasingFunction }; ;
            MarginCloseAnimation = new(new Thickness(0, -300, 0, 0), AnimationsDuration) { EasingFunction = EasingFunction };

            OpacityAnimation.Completed += (sender, e) => 
            {
                if (!IsOpen)
                {
                    Visibility = Visibility.Collapsed;
                    (App.Current.MainWindow.DataContext as MainWindowViewModel).PopupContent = null;
                    locked = false;
                }
            };
        }

        private void HidePopup(object sender, MouseButtonEventArgs e)
        {
            if (locked) return;

            IsOpen = false;
            locked = true;
        }
    }
}
