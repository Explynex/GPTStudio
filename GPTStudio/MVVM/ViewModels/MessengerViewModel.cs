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
using System.Speech.Synthesis;
using static OpenAI.GPT3.ObjectModels.SharedModels.IOpenAiModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using Microsoft.VisualBasic;
using NAudio.Wave;
using NAudio.Utils;
using GPTStudio.Utils;

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
        public RelayCommand ListenMessageCommand { get; private set; }
        public static ScrollViewer ChatScrollViewer { get; set; }

        public event Action<string> AssistantResponseCallback;

        private AudioRecorder _audioRecorder;

        private bool _isAudioRecording;
        public bool IsAudioRecording
        {
            get => _isAudioRecording;
            set => SetProperty(ref _isAudioRecording, value);
        }

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
            Chats[0].Messages.Add(new ChatGPTMessage("assistant", "The number of characters in Chinese may depend on how the character is counted. If we are talking about the number of characters, then in the modern standard of the Chinese language (unified characters) there are more than 50,000 of them. However, most of them are used very rarely, and in practice, to read and write Chinese, it is usually enough to know from 3,000 to 5,000 of the most common hieroglyphs. If we talk about the number of characters in a broad sense, then the Chinese language uses many different characters, including punctuation marks, numbers, letters, etc., and the total number of characters can be significantly higher."));
            ClearSearchBoxCommand = new RelayCommand(o => SearchBoxText = null);

            SendMessageCommand = new AsyncRelayCommand(async (o) =>
            {
                var stream = new MemoryStream();

                if (IsAudioRecording)
                {
                    IsAudioRecording = false;
                    _audioRecorder.Stop();

                    var audioResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
                    {
                        FileName = "Hello.mp3",
                        File = _audioRecorder.MemoryStream.ToArray(),
                        Model = Models.WhisperV1,
                        ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson
                    });

                    TypingMessageText = audioResult.Text;

                    _audioRecorder.Dispose();
                    return;
                }
                else if (string.IsNullOrEmpty(TypingMessageText))
                {
                    _audioRecorder = new();
                    _audioRecorder.Start();
                    IsAudioRecording = true;
                    return;
                }

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

                TypingMessageText = null;
                current.DynamicResponseCallback.Append(". . .");

                int counter = 0;
                await Task.Run(async () =>
                {
                    await foreach (var completion in completionResult)
                    {
                        if (completion.Successful)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                if (counter == 0)
                                    current.DynamicResponseCallback.Clear();

                                current.DynamicResponseCallback.Append(completion.Choices.First().Message.Content);
                                ChatScrollViewer.ScrollToBottom();
                                counter++;
                            });

                        }
                        else if (completion.Error != null)
                        {
                            throw new Exception(completion.Error.ToString());
                        }
                    }
                });

                SelectedChat.Messages[SelectedChat.Messages.Count - 1] = new ChatGPTMessage("assistant", current.DynamicResponseCallback.Text);
                current.DynamicResponseCallback.Clear();


                TypingMessageText = null;
            });

            ListenMessageCommand = new RelayCommand(o =>
            {
                SpeechSynthesizer synthesizer = new();
                synthesizer.Volume = 100;  // 0...100
                synthesizer.Rate = -1;     // -10...10

                var str = o as string;
                // Synchronous
                synthesizer.SpeakAsync(str);
            });
        }

    }
}
