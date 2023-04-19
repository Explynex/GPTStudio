using GPTStudio.Infrastructure;
using GPTStudio.Infrastructure.Azure;
using GPTStudio.Infrastructure.Models;
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
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        public string Text => _builder.ToString();
        public int Count => _builder.Length; 
        

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


        private AudioRecorder _audioRecorder;
        private LanguageDetector langDetector;

        private SpeechHandler speechHandler;

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
            langDetector = new();
            langDetector.AddLanguages("ru", "uk", "en");
            Chats = Common.BinaryDeserialize<ObservableCollection<Chat>>($"{App.UserdataDirectory}\\chats");

            ClearSearchBoxCommand = new RelayCommand(o => SearchBoxText = null);

            SendMessageCommand = new AsyncRelayCommand(async (o) =>
            {
                var api = new OpenAIClient(Config.Properties.OpenAIAPIKey);

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

                var request = new ChatRequest(SelectedChat.Messages.TakeLast(20),Model.GPT3_5_Turbo,maxTokens: 550);

                SelectedChat.Messages.Add(new ChatGPTMessage(Role.Assistant, ". . ."));

                int counter = 0;
                var current = SelectedChat.Messages[SelectedChat.Messages.Count - 1];
                TypingMessageText = null;
                List<string> list = new();
                StringBuilder sentence = new();

                await api.ChatEndpoint.StreamCompletionAsync(request, result =>
                {
                    if (String.IsNullOrEmpty(result.FirstChoice))
                        return;

                    if (counter == 0)
                        current.ChatCompletion.Clear();

                    sentence.Append(result.FirstChoice);
                    if(result.FirstChoice.ToString() == ".")
                    {
                        speechHandler.TextToSpeechQueue.Enqueue(sentence.ToString());
                        if (!speechHandler.IsSpeaking)
                            speechHandler.StartQueueSpeaking("ru-RU-SvetlanaNeural");
                        sentence.Clear();
                    }

                    current.ChatCompletion.Append(result.FirstChoice);

                    App.Current.Dispatcher.Invoke(() => ChatScrollViewer.ScrollToBottom());
                    counter++;
                });

                if(sentence.Length > 0)
                {
                    speechHandler.TextToSpeechQueue.Enqueue(sentence.ToString());
                    sentence.Clear();
                }

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
                    
                    if (sender.IsMessageListening)
                    {
                        await speechHandler.StopSpeaking();
                        return;
                    }
                    await speechHandler.StopSpeaking();

                }
                var voice = GetSpeechVoice(sender.Content.Length > 50 ? sender.Content[..50] : sender.Content);
                if (voice == null)
                {
                    return;
                }


                sender.IsMessageListening = true;
                _ = speechHandler.StartSpeaking(sender.Content, voice, (ResponseCode) => 
                {
                    sender.IsMessageListening = false;
                });
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



        /*        private async Task LoadOAuthToken()
        {
            var path = App.UserdataDirectory + "\\azurespeechtoken";
            if (_azureToken == null && File.Exists(path))
            {
                _azureToken = JsonSerializer.Deserialize<OAuthTokenInfo>(File.ReadAllText(path));
            }
            else if(_azureToken == null) _azureToken = new();

            if((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _azureToken.LastUpdateTimestamp) > 540)
            {
                _azureToken.Token               = await speechHandler.GetOAuthToken();
                _azureToken.LastUpdateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(path,JsonSerializer.Serialize<OAuthTokenInfo>(_azureToken));
            }
            speechHandler.OAuthToken = _azureToken.Token;
        }*/

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
    }
}
