using GPTStudio.MVVM.Core;
using GPTStudio.Utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using OpenAI.Models;
using OpenAI.Edits;
using GPTStudio.Infrastructure;

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

    internal sealed class ChatGPTMessage 
    {
        public Message InnerMessage { get; private set; }
        public BindableStringBuilder DynamicResponseCallback { get; set; }
        public ChatGPTMessage(Role role, string content)
        {
            if (role == Role.Assistant)
                DynamicResponseCallback = new BindableStringBuilder();
            InnerMessage = new(role, content);
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
            TextChanged?.Invoke(this, null);
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
            
            

            Chats = new()
            {
                new Chat{ Name = "Test1" },
                new Chat{ Name = "Chates"},
                new Chat{ Name = "TEST2"},
                new Chat{ Name = "Test"},
                new Chat{ Name = "Somechatwefefefefggdfgasdsadfasdfas"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
                new Chat{ Name = "Test2"},
            };

            for (int i = 0; i < 100; i++)
            {
                Chats[0].Messages.Add(new ChatGPTMessage(Role.User, "Конечно, вот пример кода на Python, который выводит на консоль \"Hello World\":\n\n```\nprint(\"Hello World!\")\n```\n\nЭтот код использует функцию `print()` для вывода строки \"Hello World!\" на консоль. Когда вы запустите этот код, вы должны увидеть сообщение \"Hello World!\" в консоли."));
            }

            for (int i = 0; i < 100; i++)
            {
                Chats[1].Messages.Add(new ChatGPTMessage(Role.User, "\n```\nprint(\"Hello World!\")\n```\n\nЭтот код используе)` для вывода ст апуститеHello World!\\ в консоли."));
            }

            Chats[0].Messages.Add(new ChatGPTMessage(Role.User, "```#include <iostream>\r\n\r\nusing namespace std;\r\n\r\nint main()\r\n{\r\n    cout << \"Hello world!\" << endl;\r\n    return 0;\r\n}```"));
            ClearSearchBoxCommand = new RelayCommand(o => SearchBoxText = null);

            SendMessageCommand = new AsyncRelayCommand(async (o) =>
            {
                var api = new OpenAIClient(Config.Properties.OpenAIAPIKey);
                /*                var autoDetectSourceLanguageConfig =
                                    AutoDetectSourceLanguageConfig.FromLanguages(
                                        new string[] { "en-US", "de-DE", "zh-CN", "ru-RU" });

                                using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                                using (var recognizer = new SpeechRecognizer(
                                    speechConfig,
                                    autoDetectSourceLanguageConfig,
                                    audioConfig))
                                {
                                    recognizer.Recognized += (sender, e) => { TypingMessageText += e.Result.Text; };
                                    await recognizer.StartContinuousRecognitionAsync();
                                    await Task.Delay(10000);
                                    await recognizer.StopContinuousRecognitionAsync();
                                    // var result = await api.EditsEndpoint.CreateEditAsync(new EditRequest(TypingMessageText, "Correct punctuation marks without translation"));
                                    // TypingMessageText = result.ToString();
                                    return;
                                }*/

                if (IsAudioRecording)
                {
                    IsAudioRecording = false;
                    _audioRecorder.Stop();
                    // File.WriteAllBytes("D:\\output.mp3", _audioRecorder.MemoryStream.ToArray());

                    _audioRecorder.MemoryStream.Position = 0;
                    var audioResult = await api.AudioEndpoint.CreateTranscriptionAsync(new OpenAI.Audio.AudioTranscriptionRequest(_audioRecorder.MemoryStream, "hello.wav"));

                    TypingMessageText = audioResult;

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

                SelectedChat.Messages.Add(new ChatGPTMessage(Role.User, TypingMessageText));
                ChatScrollViewer.ScrollToBottom();

                var request = new ChatRequest(SelectedChat.Messages.Select(o => o.InnerMessage),Model.GPT3_5_Turbo,maxTokens: 550);
                SelectedChat.Messages.Add(new ChatGPTMessage(Role.Assistant, ""));

                int counter = 0;
                var current = SelectedChat.Messages[SelectedChat.Messages.Count - 1];
                TypingMessageText = null;
                current.DynamicResponseCallback.Append(". . .");

                await api.ChatEndpoint.StreamCompletionAsync(request, result =>
                {
                    if (String.IsNullOrEmpty(result.FirstChoice))
                        return;

                    if (counter == 0)
                        current.DynamicResponseCallback.Clear();

                    current.DynamicResponseCallback.Append(result.FirstChoice);
                    App.Current.Dispatcher.Invoke(() => ChatScrollViewer.ScrollToBottom());
                    counter++;
                });

                SelectedChat.Messages[SelectedChat.Messages.Count - 1] = new ChatGPTMessage(Role.Assistant, current.DynamicResponseCallback.Text);
                current.DynamicResponseCallback.Clear();
            });

            ListenMessageCommand = new AsyncRelayCommand(async(o) =>
            {
                var speechConfig = SpeechConfig.FromSubscription(Config.Properties.AzureAPIKey, Config.Properties.AzureSpeechRegion);
                speechConfig.SpeechSynthesisVoiceName = "en-US-EricNeural";


                using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
                {
                    using var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(o as string);
                }
            });
        }

    }
}
