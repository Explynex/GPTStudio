using GPTStudio.MVVM.Core;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System;
using System.Collections.ObjectModel;
using OpenAI.GPT3.ObjectModels;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Controls;

namespace GPTStudio.MVVM.ViewModels
{
    internal sealed class Chat 
    {
        public Chat()
        {
            Messages = new ObservableCollection<ChatGPTMessage>();
        }
        public string Name { get; set; }
        public ObservableCollection<ChatGPTMessage> Messages { get; set; }
        public string CreatedTimestamp { get; set; }
    }

    internal sealed class ChatGPTMessage : ChatMessage
    {
        public BindableStringBuilder DynamicResponseCallback { get; set; }
        public ChatGPTMessage(string role, string content) : base(role, content)
        {
            if (role != StaticValues.ChatMessageRoles.User)
                DynamicResponseCallback = new BindableStringBuilder();
        }
    }

    public class BindableStringBuilder : INotifyPropertyChanged
    {
        private readonly StringBuilder _builder = new StringBuilder();

        private EventHandler<EventArgs> TextChanged;

        public string Text
        {
            get { return _builder.ToString(); }
        }

        public int Count
        {
            get { return _builder.Length; }
        }

        public void Append(string text)
        {
            _builder.Append(text);
            if (TextChanged != null)
                TextChanged(this, null);
            RaisePropertyChanged(() => Text);
        }

        public void AppendLine(string text)
        {
            _builder.AppendLine(text);
            if (TextChanged != null)
                TextChanged(this, null);
            RaisePropertyChanged(() => Text);
        }

        public void Clear()
        {
            _builder.Clear();
            if (TextChanged != null)
                TextChanged(this, null);
            RaisePropertyChanged(() => Text);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        public void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                return;
            }

            var handler = PropertyChanged;

            if (handler != null)
            {
                var body = propertyExpression.Body as MemberExpression;
                if (body != null)
                    handler(this, new PropertyChangedEventArgs(body.Member.Name));
            }
        }

        #endregion


    }

    internal sealed class MessengerViewModel : ObservableObject
    {
        public RelayCommand ClearSearchBoxCommand { get; private set; }
        public AsyncRelayCommand SendMessageCommand { get; private set; }
        public AsyncRelayCommand ListenMessageCommand { get; private set; }
        public static ScrollViewer ChatScrollViewer { get; set; }

        public event Action<string> AssistantResponseCallback;


        private string _searchBoxText;
        public string SearchBoxText
        {
            get => _searchBoxText;
            set => SetProperty(ref _searchBoxText, value);
        }

        private string _typingMessageText;
        public string TypingMessageText
        {
            get => _typingMessageText;
            set => SetProperty(ref _typingMessageText, value);
        }

        private ObservableCollection<Chat> _chats;
        public ObservableCollection<Chat> Chats
        {
            get => _chats;
            set => SetProperty(ref _chats, value);
        }

        private Chat _selecedChat;
        public Chat SelectedChat
        {
            get => _selecedChat;
            set => SetProperty(ref _selecedChat, value);
        }


        public MessengerViewModel()
        {
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = "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"
            });

            Chats = new()
            {
                new Chat{ Name = "Test1" },
                new Chat{ Name = "Chates"},
                new Chat{ Name = "GPTSemen"},
                new Chat{ Name = "Nicecock"},
                new Chat{ Name = "Somechatwefefefefggdfgasdsadfasdfas"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
            };
            Chats[0].Messages.Add(new ChatGPTMessage("assistant", "Количество символов в китайском языке может зависеть от того, как считать символ. Если мы говорим о количество иероглифов, то в современном стандарте китайского языка (унифицированных иероглифах) их более 50 000. Однако, большинство из них используются очень редко, а на практике для чтения и написания китайского языка обычно достаточно знать от 3 000 до 5 000 наиболее распространенных иероглифов. Если мы говорим о количестве символов в широком смысле, то китайский язык использует множество различных знаков, включая знаки препинания, числа, буквы и т.д., и общее количество символов может быть существенно больше."));
            ClearSearchBoxCommand = new RelayCommand(o => SearchBoxText = null);

            SendMessageCommand = new AsyncRelayCommand(async (o) =>
            {
                if (string.IsNullOrEmpty(TypingMessageText))
                    return;

                SelectedChat.Messages.Add(new ChatGPTMessage(StaticValues.ChatMessageRoles.User, TypingMessageText));
                ChatScrollViewer.ScrollToBottom();

                var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
                {
                    Messages = SelectedChat.Messages.Cast<ChatMessage>().ToList(),
                    Model = Models.ChatGpt3_5Turbo,
                    MaxTokens = 250//optional
                });

                SelectedChat.Messages.Add(new ChatGPTMessage(StaticValues.ChatMessageRoles.Assistant, ""));
                var current = SelectedChat.Messages[SelectedChat.Messages.Count - 1];

                await Task.Run(async () =>
                {
                    await foreach (var completion in completionResult)
                    {
                        if (completion.Successful)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                current.DynamicResponseCallback.Append(completion.Choices.First().Message.Content);
                                ChatScrollViewer.ScrollToBottom();
                            });

                        }
                        else if (completion.Error != null)
                        {
                            throw new Exception(completion.Error.ToString());
                        }
                    }
                });

                current.Content = current.DynamicResponseCallback.Text;

                TypingMessageText = null;
            }) ;

            ListenMessageCommand = new AsyncRelayCommand(async (o) =>
            {
                SpeechSynthesizer synthesizer;
            });
        }

    }
}
