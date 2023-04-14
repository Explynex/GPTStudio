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
            MessengerViewModel.ChatScrollViewer.Loaded += (sender, e) => MessengerViewModel.ChatScrollViewer.ScrollToEnd();
        }

        private async void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           /* await Task.Run(async () =>
            {
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = "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"
                });

                var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
    {
                    new(StaticValues.ChatMessageRoles.User, str),
    },
                    Model = Models.ChatGpt3_5Turbo,
                    MaxTokens = 150//optional
                });

                await foreach (var completion in completionResult)
                {
                    if (completion.Successful)
                    {
                        Update(completion.Choices.First().Message.Content);
                    }
                    else
                    {
                        if (completion.Error == null)
                        {
                            throw new Exception("Unknown Error");
                        }

                        Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                    }
                }
            });*/
            
        }
        private void messages_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
/*            if (MessengerViewModel.ChatScrollViewer.ContentVerticalOffset != 0 && e.VerticalChange != 0 && MessengerViewModel.ChatScrollViewer.ContentVerticalOffset < 300)
            {
                messages.ItemsSource = viewModel.SelectedChat.Messages.Take(messages.Items.Count + 10);
                MessengerViewModel.ChatScrollViewer.ScrollToVerticalOffset(600);
            }*/



        }
    }

}
