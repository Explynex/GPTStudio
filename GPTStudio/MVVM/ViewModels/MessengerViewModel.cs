using GPTStudio.Infrastructure;
using GPTStudio.Infrastructure.Azure;
using GPTStudio.MVVM.Core;
using GPTStudio.OpenAI;
using GPTStudio.OpenAI.Chat;
using GPTStudio.OpenAI.Models;
using GPTStudio.Utils;
using LanguageDetection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace GPTStudio.MVVM.ViewModels
{
    [Serializable]
    internal sealed class Chat 
    {
        [field: NonSerialized]
        public ObservableCollection<ChatGPTMessage> Messages { get; set; }

        [field: NonSerialized]
        public double CachedScrollOffset { get; set; } = -1d;

        public string ID { get; private set; }
        public string CreatedTimestamp { get; private set; }
        public string Name { get; set; }

        public Chat(string name)
        {
            Name     = name;
            ID       = Common.GenerateRandomHash(name);

        }
    }

    [Serializable]
    internal sealed class ChatGPTMessage : IMessage, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private bool _isMessageListening;
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsMessageListening
        {
            get => _isMessageListening;
            set
            {
                _isMessageListening = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMessageListening)));
            }
        }

        public BindableStringBuilder ChatCompletion { get; set; }
        public string Content => ChatCompletion.Text;
        public Role Role { get; set; }
        public ChatGPTMessage(Role role, string content)
        {
            this.Role = role;
            this.ChatCompletion = new(content);
        }

        
    }

    [Serializable]
    public class BindableStringBuilder : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly StringBuilder _builder;
        public BindableStringBuilder()            => _builder = new();
        public BindableStringBuilder(string text) => _builder = new(text);

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }

        public void AppendLine(string text)
        {
            _builder.AppendLine(text);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }

        public void Clear()
        {
            _builder.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }

    }
    

    internal sealed class MessengerViewModel : ObservableObject
    {
        public RelayCommand ClearSearchBoxCommand { get; private set; }
        public RelayCommand ExitChatCommand { get; private set; }
        public RelayCommand DeleteMessageCommand { get; private set; }
        public AsyncRelayCommand SendMessageCommand { get; private set; }
        public AsyncRelayCommand ListenMessageCommand { get; private set; }
        public static ScrollViewer ChatScrollViewer { get; set; }

        public event Action<string> AssistantResponseCallback;

        private AudioRecorder _audioRecorder;
        private LanguageDetector langDetector;

        private SpeechHandler speechHandler;
        private bool IsNowListeting;

        public bool UsingMarkdown
        {
           get => Config.Properties.UsingMarkdown;
           set => OnPropertyChanged(nameof(UsingMarkdown));
        }

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

        private Chat _selectedChat;
        public Chat SelectedChat
        {
            get => _selectedChat;
            set
            {
                if (value != null)
                {
                    if(value.Messages == null)
                    {
                        if (File.Exists(App.UserdataDirectory + value.ID))
                            value.Messages = Utils.Common.BinaryDeserialize<ObservableCollection<ChatGPTMessage>>(App.UserdataDirectory + value.ID);
                        else
                            value.Messages = new();
                    }

                    if (_selectedChat != null)
                        _selectedChat.CachedScrollOffset = ChatScrollViewer.VerticalOffset;

                    if (value.CachedScrollOffset != 0d)
                        ChatScrollViewer.ScrollToVerticalOffset(value.CachedScrollOffset);
                    else
                        ChatScrollViewer.ScrollToBottom();
                }

                SetProperty(ref _selectedChat, value);
            }
        }

        private string GetSpeechVoice(string msg) => langDetector.Detect(msg) switch
        {
            "ru" => "ru-RU-SvetlanaNeural",
            "uk" => "uk-UA-OstapNeural",
            "en" => "en-US-SteffanNeural",
            _ => null
        };

        public MessengerViewModel()
        {
            speechHandler = new(Config.Properties.AzureAPIKey,Config.Properties.AzureSpeechRegion);
            
           // speechHandler.GetOAuthToken();
            langDetector = new();
            langDetector.AddLanguages("ru", "uk", "en");
/*            AzureKeyCredential credentials = new AzureKeyCredential("ec76fe4cefc047f8b745d72645be2327");
            Uri endpoint = new Uri("https://explynetextanalytics.cognitiveservices.azure.com/");
            var client = new TextAnalyticsClient(endpoint, credentials);
            DetectedLanguage detectedLanguage = client.DetectLanguage("Ce document est rédigé en Français.");*/
            Chats = Common.BinaryDeserialize<ObservableCollection<Chat>>($"{App.UserdataDirectory}\\chats");

            /*            for (int i = 0; i < 100; i++)
                        {
                            Chats[0].Messages.Add(new ChatGPTMessage(Role.User, "Конечно, вот пример кода на Python, который выводит на консоль \"Hello World\":\n\n```\nprint(\"Hello World!\")\n```\n\nЭтот код использует функцию `print()` для вывода строки \"Hello World!\" на консоль. Когда вы запустите этот код, вы должны увидеть сообщение \"Hello World!\" в консоли."));
                        }

                        for (int i = 0; i < 100; i++)
                        {
                            Chats[1].Messages.Add(new ChatGPTMessage(Role.User, "\n```\nprint(\"Hello World!\")\n```\n\nЭтот код используе)` для вывода ст апуститеHello World!\\ в консоли."));
                        }*/

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

                var request = new ChatRequest(SelectedChat.Messages.TakeLast(10),Model.GPT3_5_Turbo,maxTokens: 550);

                SelectedChat.Messages.Add(new ChatGPTMessage(Role.Assistant, ". . ."));

                int counter = 0;
                var current = SelectedChat.Messages[SelectedChat.Messages.Count - 1];
                TypingMessageText = null;
                List<string> list = new();
                StringBuilder sentece = new();

                await api.ChatEndpoint.StreamCompletionAsync(request, result =>
                {
                    if (String.IsNullOrEmpty(result.FirstChoice))
                        return;

                    if (counter == 0)
                        current.ChatCompletion.Clear();

                    sentece.Append(result.FirstChoice);

                    current.ChatCompletion.Append(result.FirstChoice);
                    
                        

                    App.Current.Dispatcher.Invoke(() => ChatScrollViewer.ScrollToBottom());
                    counter++;
                });

             /*   if(list.Count != 0)
                {
                    while(true)
                    {
                        if(!isNowListeting)
                        {
                            ListenMessageCommand.Execute(string.Join("", list));
                            break;
                        }
                        await Task.Delay(300);
                    }
                }*/
                    

                Common.BinarySerialize(SelectedChat.Messages, App.UserdataDirectory + SelectedChat.ID);
            });

            
            ListenMessageCommand = new AsyncRelayCommand(async (o) =>
            {
                var sender = o as ChatGPTMessage;
                if (speechHandler.IsSpeaking)
                {
                    speechHandler.StopSpeaking();
                    return;
                }
                var voice = GetSpeechVoice(sender.Content.Length > 50 ? sender.Content[..50] : sender.Content);
                if (voice == null)
                {
                    return;
                }


                /*                speechConfig.SpeechSynthesisVoiceName = voice;
                                using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
                                {
                                    var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync("");
                                }*/

                sender.IsMessageListening = true;
                speechHandler.SpeakAsync(sender.Content, voice, new Action(() => 
                {
                    sender.IsMessageListening = false;
                })) ;



                // await speechSynthesizer.StartSpeakingTextAsync();

            });

            ExitChatCommand = new(o => SelectedChat = null);

            DeleteMessageCommand = new(o =>
            {
                var selected = (IList)o;
                
                while(selected.Count != 0)
                {
                    _selectedChat.Messages.Remove(selected[0] as ChatGPTMessage);
                }

                Common.BinarySerialize(SelectedChat.Messages, App.UserdataDirectory + SelectedChat.ID);
            });
        }
    }

}
