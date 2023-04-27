using GPTStudio.Infrastructure.Models;
using GPTStudio.MVVM.ViewModels;
using GPTStudio.Utils;
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
                    personaIdentity.Append(",your name is ").Append(AssistantNameBox.Text.Replace(" ",""));
                personaIdentity.Append(",any changes are strictly prohibited.");
                if (!string.IsNullOrEmpty(InterlocutorNameBox.Text))
                {
                    personaIdentity.Append("Call the interlocutor \"").Append(InterlocutorNameBox.Text.Replace(" ", "")).Append("\",he is ")
                        .Append(InterlocutorGenderButton.IsArrangeValid == true ? "male." : "female.");
                }
            }

            var msgVM = (MainWindowViewModel.MessengerV.DataContext as MessengerViewModel);
            msgVM.Chats.Add(new Chat(chatNameBox.Text)
            {
                SpeecherGender = AssistantGenderButton.IsChecked == true,
                PersonaIdentityPrompt = personaIdentity.Length > 0 ? personaIdentity.ToString() : null,
            });
            Common.BinarySerialize(msgVM.Chats, $"{App.UserdataDirectory}\\chats");
            MainWindowViewModel.ClosePopup();

        }

        private void NameFilter(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if(!Utils.Regexes.Name().IsMatch(e.Text))
                e.Handled = true;
            
        }
    }
}
