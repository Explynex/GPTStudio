using GPTStudio.MVVM.ViewModels;
using System;
using System.Windows.Controls;

namespace GPTStudio.MVVM.View.Controls;

public partial class ChoiceView : Grid
{
    public Action SuccessAction { get; set; }
    public bool AutoClose { get; set; } = true;
    public ChoiceView() => InitializeComponent();
    private void No_Click(object sender, System.Windows.RoutedEventArgs e) => MainWindowViewModel.ClosePopup();
    

    private void Yes_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SuccessAction?.Invoke();
        if(AutoClose)
            MainWindowViewModel.ClosePopup();
    }
}
