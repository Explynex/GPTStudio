using GPTStudio.MVVM.ViewModels;
using GPTStudio.OpenAI.Chat;
using GPTStudio.Utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GPTStudio.Infrastructure.Models
{
    [Serializable]
    internal sealed class Chat
    {
        public static bool NeedToUpdate { get; set; }

        #region Temp
        [field: NonSerialized]
        public ObservableCollection<ChatGPTMessage> Messages { get; set; }

        [field: NonSerialized]
        public string TypingMessageText { get; set; }

        [field: NonSerialized]
        public double CachedScrollOffset { get; set; }
        #endregion

        #region Model settings properties & validators
        private int _tokens = 512;
        private double _temperature = 0.7d, _topP = 1d, _freqPenalty = 0d, _presPenalty = 0d;
        public int Tokens
        {
            get => _tokens;
            set
            {
                if (value > 0 && value <= 2048)
                    SetModelSetting(ref _tokens, ref value);
            }
        }
        public double Temperature
        {
            get => _temperature;
            set
            {
                if (value >= 0d && value <= 1.0d)
                    SetModelSetting(ref _temperature, ref value);
            }
        }
        public double TopP
        {
            get => _topP;
            set
            {
                if (value >= 0d && value <= 1.0d)
                    SetModelSetting(ref _topP, ref value);
            }
        }
        public double FreqPenalty
        {
            get => _freqPenalty;
            set
            {
                if (value >= 0d && value <= 2.0d)
                    SetModelSetting(ref _freqPenalty, ref value);
            }
        }
        public double PresPenalty
        {
            get => _presPenalty;
            set
            {
                if (value >= 0d && value <= 2.0d)
                    SetModelSetting(ref _presPenalty, ref value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetModelSetting<T>(ref T field, ref T value)
        {
            if (field?.Equals(value) == true) return;
            field = value;
            NeedToUpdate = true;
        }
        #endregion

        private string _name, _systemMessagePrompt;
        public string ID { get; private set; }
        public TimeSpan CreatedTimestamp { get; private set; }
        public string Name
        {
            get => _name;
            set
            {
                if (!string.IsNullOrEmpty(value) && !string.Equals(value, _name))
                    SetModelSetting(ref _name, ref value);
            }
        }
        public bool SpeecherGender { get; set; }
        public string PersonaIdentityPrompt { get; set; }
        public string SystemMessagePrompt
        {
            get => _systemMessagePrompt;
            set => SetModelSetting(ref _systemMessagePrompt, ref value);
        }

        public Chat(string name)
        {
            _name = name;
            ID = Common.GenerateRandomHash(name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Save() => Common.BinarySerialize((MainWindowViewModel.MessengerV.DataContext as MessengerViewModel).Chats, $"{App.UserdataDirectory}\\chats");
    }

    [Serializable]
    internal sealed class ChatGPTMessage : IMessage, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private bool _isMessageListening;
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private int _tokens;

        public int Tokens
        {
            get => _tokens;
            set
            {
                _tokens = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tokens)));
            }
        }

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
}
