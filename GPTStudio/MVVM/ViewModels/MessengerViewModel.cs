using GPTStudio.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.MVVM.ViewModels
{
    class MessengerViewModel : ObservableObject
    {
        public RelayCommand ClearSearchBoxCommand { get; private set; }
        private string _searchBoxText;
        public string SearchBoxText
        {
            get => _searchBoxText;
            set => SetProperty(ref _searchBoxText, value);
        }

        public MessengerViewModel()
        {
            ClearSearchBoxCommand = new RelayCommand(o =>
            {
                SearchBoxText = null;
            });
        }

    }
}
