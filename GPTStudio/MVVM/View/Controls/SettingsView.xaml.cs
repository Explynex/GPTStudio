using GPTStudio.Infrastructure;
using GPTStudio.Utils;
using System.Windows.Controls;
using System.Windows.Media;

namespace GPTStudio.MVVM.View.Controls;

public partial class SettingsView : Grid
{
    internal static Infrastructure.Models.Properties Properties => Config.Properties;
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new ViewModels.SettingsViewModel();
        Loaded += (sender, e) => Config.NeedToUpdate = true;
    }

    private void openAIKeyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!Regexes.OpenAIApiKey().IsMatch(openAIKeyBox.Text))
        {
            if (openAIKeyBox.BorderBrush != Brushes.PaleVioletRed)
                openAIKeyBox.BorderBrush = Brushes.PaleVioletRed;
        }
        else
        {
            if(openAIKeyBox.Text != Config.Properties.OpenAIAPIKey)
            {
                Config.Properties.OpenAIAPIKey = openAIKeyBox.Text;
                Config.OpenAIClientApi         = new(openAIKeyBox.Text);
            }

            openAIKeyBox.BorderBrush = Brushes.Transparent;
        }
    }
}
