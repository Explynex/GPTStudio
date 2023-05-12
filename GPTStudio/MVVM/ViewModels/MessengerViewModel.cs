using GPTStudio.Infrastructure;
using GPTStudio.Infrastructure.Azure;
using GPTStudio.Infrastructure.Models;
using GPTStudio.MVVM.Core;
using GPTStudio.MVVM.View.Controls;
using GPTStudio.OpenAI.Chat;
using GPTStudio.OpenAI.Models;
using GPTStudio.OpenAI.Tokenizer;
using GPTStudio.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace GPTStudio.MVVM.ViewModels;

[Serializable]
internal sealed class BindableStringBuilder : INotifyPropertyChanged
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

internal sealed class MessengerViewModel : ObservableObject, IDisposable
{
    public RelayCommand ClearSearchBoxCommand { get; private set; }
    public RelayCommand ExitChatCommand { get; private set; }
    public RelayCommand DeleteMessageCommand { get; private set; }
    public RelayCommand AddNewChatCommand { get; private set; }
    public AsyncRelayCommand SendMessageCommand { get; private set; }
    public AsyncRelayCommand ListenMessageCommand { get; private set; }
    public RelayCommand DeleteChatCommand { get; private set; }
    public RelayCommand ClearMessagesCommand { get; private set; }
    public static ScrollViewer ChatScrollViewer { get; set; }


    private AudioRecorder _audioRecorder;
    private GPTTokenizer _tokenizer;
    private ICollectionView SearchFilter;
    private readonly SpeechHandler speechHandler;

    private bool IsBusy;

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
        set
        {
            SetProperty(ref _searchBoxText, value);

            if(value != null)
                SearchFilter.Refresh();
        }
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
                    {
                        value.Messages = Utils.Common.BinaryDeserialize<ObservableCollection<ChatGPTMessage>>(App.UserdataDirectory + value.ID);

                    }
                    else
                        value.Messages = new();
                }

                if (_selectedChat != null)
                    _selectedChat.CachedScrollOffset = ChatScrollViewer.VerticalOffset;


                if (value.CachedScrollOffset != 0d)
                    ChatScrollViewer.ScrollToVerticalOffset(value.CachedScrollOffset);
                else
                    ChatScrollViewer.ScrollToBottom();
                SearchBoxText       = null;
                SearchFilter        = CollectionViewSource.GetDefaultView(value.Messages);
                SearchFilter.Filter = FilterPredicate;
            }

            SetProperty(ref _selectedChat, value);
        }
    }

    private static bool GetSpeechVoice(string msg, out string voice)
    {
        var lang = Config.LangDetector?.Detect(msg.Length > 70 ? msg[..70] : msg);

        if (lang == null)
        {
            voice = null;
            return false;
        }
            
        voice = Config.LanguagesConfig[lang].SelectedSpeecher;
        return true;
    }

    private bool FilterPredicate(object value)
    {
        if (String.IsNullOrEmpty(_searchBoxText))
            return true;

        if ((value is Chat chat && chat.Name.Contains(_searchBoxText,StringComparison.InvariantCultureIgnoreCase))
            || value is ChatGPTMessage msg && msg.Content.Contains(_searchBoxText, StringComparison.InvariantCultureIgnoreCase))
        {
           return true;
        }

        return false;
    }

    private void InvalidOpenAIKey()
    {
            Utils.Presentation.OpenChoicePopup("Invalid OpenAI API-key", "Incorrect API key from OpenAI services specified. Open settings?",
                () => (App.Current.MainWindow.DataContext as MainWindowViewModel).SettingsCommand.Execute(null), false);
    }

    public void RefreshCollection(bool autoscroll = true)
    {
        SearchBoxText = null;
        if((SearchFilter as ListCollectionView).Count != (SearchFilter.SourceCollection as IList).Count)
            SearchFilter.Refresh();
        if(SelectedChat != null && autoscroll)
        {
            ChatScrollViewer.ScrollToEnd();
        }
    }

    public void Dispose()
    {
        speechHandler.Dispose();
        _audioRecorder.Dispose();
        _tokenizer.Dispose();
    }

    public MessengerViewModel()
    {
        speechHandler = new(Config.Properties.AzureAPIKey,Config.Properties.AzureSpeechRegion);

        if (!File.Exists($"{App.UserdataDirectory}voices"))
            _ = speechHandler.GetVoicesListAsync();

        _tokenizer = new(File.ReadAllText("merges.txt"));

        Chats = Common.BinaryDeserialize<ObservableCollection<Chat>>($"{App.UserdataDirectory}\\chats") ?? new();
        SearchFilter = CollectionViewSource.GetDefaultView(Chats);
        SearchFilter.Filter = FilterPredicate;

        ClearSearchBoxCommand = new(o =>  RefreshCollection());
        SendMessageCommand    = new(async (o) =>
        {
            if (IsBusy) return;

            if (Config.OpenAIClientApi == null)
            {
                InvalidOpenAIKey();
                return;
            }
            var currentChat = SelectedChat;

            if (IsAudioRecording)
            {
                IsAudioRecording = false;
                _audioRecorder.Stop();
                // File.WriteAllBytes("D:\\output.mp3", _audioRecorder.MemoryStream.ToArray());

                _audioRecorder.MemoryStream.Position = 0;
                var audioResult = await Config.OpenAIClientApi.AudioEndpoint.CreateTranscriptionAsync(new OpenAI.Audio.AudioTranscriptionRequest(_audioRecorder.MemoryStream, "hello.wav"));

                currentChat.TypingMessageText = audioResult;
                OnPropertyChanged(nameof(SelectedChat));

                _audioRecorder.Dispose();
                return;
            }
            else if (string.IsNullOrEmpty(currentChat.TypingMessageText))
            {
                _audioRecorder = new();
                _audioRecorder.Start();
                IsAudioRecording = true;
                return;
            }

            SelectedChat.Messages.Add(new ChatGPTMessage(Role.User, currentChat.TypingMessageText) { Tokens = _tokenizer.Calculate(currentChat.TypingMessageText)});
            ChatScrollViewer.ScrollToBottom();

            #region Calculating tokens
            // 4097 tokens max for GPT-3.5-Turbo
            int tokensCount = 0;
            var msgList = new List<ChatGPTMessage>();
            if (!string.IsNullOrEmpty(SelectedChat.PersonaIdentityPrompt) || !string.IsNullOrEmpty(SelectedChat.SystemMessagePrompt))
            {
                var system = SelectedChat.PersonaIdentityPrompt + SelectedChat.SystemMessagePrompt;
                msgList.Add(new(Role.System, system));
                tokensCount = _tokenizer.Calculate(system);
            }

            for (int i = SelectedChat.Messages.Count-1,insertIndex = tokensCount == 0 ? 0 : 1; i >= 0; i--)
            {
                var msg = SelectedChat.Messages[i];

                if ((tokensCount + msg.Tokens) > 4097)
                    break;

                tokensCount += msg.Tokens;
                msgList.Insert(insertIndex, SelectedChat.Messages[i]);
            } 
            #endregion

            var request = new ChatRequest(msgList, Model.GPT3_5_Turbo,SelectedChat.Temperature,SelectedChat.TopP,null,null,SelectedChat.Tokens,SelectedChat.PresPenalty,SelectedChat.FreqPenalty);

            SelectedChat.Messages.Add(new ChatGPTMessage(Role.Assistant, ". . ."));

            bool cleanWaiting = false;
            var currentMsg = currentChat.Messages[^1];
            currentChat.TypingMessageText = null;
            OnPropertyChanged(nameof(SelectedChat));
            StringBuilder sentence = new();

            speechHandler.CompletionEvent = (response) => currentMsg.IsMessageListening = false;
            speechHandler.TextToSpeechQueue.Clear();

            #region Streaming response
            IsBusy = true;

            try
            {
                await Config.OpenAIClientApi.ChatEndpoint.StreamCompletionAsync(request, result =>
                {
                    if (String.IsNullOrEmpty(result.FirstChoice))
                        return;

                    if (!cleanWaiting)
                    {
                        currentMsg.ChatCompletion.Clear();
                        cleanWaiting = true;
                    }

                    if (Config.Properties.AutoSpeakGPTResponses)
                    {
                        sentence.Append(result.FirstChoice);

                        if (sentence.Length > 2 && Regexes.Sentence().IsMatch(sentence.ToString()))
                        {
                            var chunkIndex = sentence.IndexOf(' ', true);
                            var chunk = sentence.ToString(chunkIndex + 1, (sentence.Length - chunkIndex) - 1);
                            var textSentence = sentence.Remove(chunkIndex, sentence.Length - chunkIndex).ToString();

                            SpeechChunk(textSentence);
                            sentence.Append(chunk);
                        }
                        else if (result.FirstChoice[^1] == '\n')
                            SpeechChunk(sentence.ToString());

                    }

                    currentMsg.ChatCompletion.Append(result.FirstChoice);

                    App.Current?.Dispatcher.Invoke(() => ChatScrollViewer.ScrollToBottom());
                });
            }
            catch(HttpRequestException e)
            {
                if(e.StatusCode == System.Net.HttpStatusCode.Unauthorized && e.Message.Contains("\"code\": \"invalid_api_key\""))
                    InvalidOpenAIKey();
                
                SelectedChat.Messages.Remove(SelectedChat.Messages[^1]);
                IsBusy = false;
                return;
            }

            currentMsg.Tokens = _tokenizer.Calculate(currentMsg.ChatCompletion.Text);

            if (sentence.Length > 0)
                SpeechChunk(sentence.ToString());
            #endregion

            IsBusy = false;
            Common.BinarySerialize(currentChat.Messages, App.UserdataDirectory + currentChat.ID);


            void SpeechChunk(string chunk)  
            {
                speechHandler.TextToSpeechQueue.Enqueue(chunk);
                if (!speechHandler.IsSpeaking && GetSpeechVoice(chunk, out string voice))
                {
                    currentMsg.IsMessageListening = true;
                    speechHandler.StartQueueSpeaking(voice, (200, 8));
                }
                sentence.Clear();
            }
        });
        ListenMessageCommand  = new(async (o) =>
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

            if(!GetSpeechVoice(sender.Content,out string voice))
            {
                sender.IsMessageListening = false;
                return;
            }

            sender.IsMessageListening = true;
            speechHandler.CompletionEvent = (Response) => sender.IsMessageListening = false;
            _ = speechHandler.StartSpeaking(sender.Content, voice);
        });

        ExitChatCommand = new(o => 
        {
            SelectedChat = null;
            SearchFilter = CollectionViewSource.GetDefaultView(Chats);
            RefreshCollection();
        });

        DeleteMessageCommand = new(o =>
        {
            var selected = (IList)o;
            
            while(selected.Count != 0)
            {
                _selectedChat.Messages.Remove(selected[0] as ChatGPTMessage);
            }

            Common.BinarySerialize(SelectedChat.Messages, App.UserdataDirectory + SelectedChat.ID);
        });

        DeleteChatCommand = new(o => 
        {
            Presentation.OpenChoicePopup($"Deleting chat «{(o as Chat).Name}»",
                "Are you sure you want to delete this chat? It will be impossible to undo the action.",
                DeleteAction);
            
            void DeleteAction()
            {
                Chats.Remove(o as Chat);
                Common.BinarySerialize(Chats, $"{App.UserdataDirectory}\\chats");
            }
        });

        ClearMessagesCommand = new(o =>
        {

            if (o is Chat chat)
            {
                chat.Messages?.Clear();
                chat.CachedScrollOffset = -1d;
                if (File.Exists(App.UserdataDirectory + chat.ID))
                    File.Delete(App.UserdataDirectory + chat.ID);
            }
        });

        AddNewChatCommand = new(o =>
        {
            (App.Current.MainWindow.DataContext as MainWindowViewModel).PopupContent  = new ChatAddingView();
            (App.Current.MainWindow.DataContext as MainWindowViewModel).IsPopupActive = true;
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
