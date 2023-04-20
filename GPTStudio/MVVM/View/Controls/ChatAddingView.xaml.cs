using NTextCat.Commons;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GPTStudio.MVVM.View.Controls
{
    public partial class ChatAddingView : Grid
    {
        public ChatAddingView()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(chatNameBox.Text))
            {
                var defColor = chatNameTitle.Foreground;
                for (int i = 0; i < 2; i++)
                {
                    chatNameTitle.Foreground = System.Windows.Media.Brushes.PaleVioletRed;
                    await Task.Delay(130);
                    chatNameTitle.Foreground = defColor;
                    await Task.Delay(130);
                }
                return;
            }

            StringBuilder personaIdentity = new();
            if(advanced.IsChecked == true)
            {
                personaIdentity.Append(AssistantGenderButton.IsChecked == true ? "You are female" : "You are male");
                if (!string.IsNullOrEmpty(AssistantNameBox.Text))
                    personaIdentity.Append(",your name is ").Append(AssistantNameBox.Text);
                personaIdentity.Append(",any changes are strictly prohibited.");
                if (!string.IsNullOrEmpty(InterlocutorNameBox.Text))
                {
                    personaIdentity.Append("Call the interlocutor \"").Append(InterlocutorNameBox.Text).Append("\",he is ")
                        .Append(InterlocutorGenderButton.IsArrangeValid == true ? "male" : "female");
                }
                    
            }
        }
    }
}
