using GPTStudio.MVVM.Core;
using System.Collections.ObjectModel;

namespace GPTStudio.MVVM.ViewModels
{
    internal sealed class Chat
    {
        public string Name { get; set; }
        public ObservableCollection<string> Messages { get; set; }
        public string CreatedTimestamp { get; set; }
    }

    internal sealed class MessengerViewModel : ObservableObject
    {
        public RelayCommand ClearSearchBoxCommand { get; private set; }
        private string _searchBoxText;
        public string SearchBoxText
        {
            get => _searchBoxText;
            set => SetProperty(ref _searchBoxText, value);
        }

        private ObservableCollection<Chat> _chats;
        public ObservableCollection<Chat> Chats
        {
            get => _chats;
            set => SetProperty(ref _chats, value);
        }

        public MessengerViewModel()
        {
            Chats = new()
            {
                new Chat{ Name = "Test1"},
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

            ClearSearchBoxCommand = new RelayCommand(o => SearchBoxText = null);
        }

    }
}
