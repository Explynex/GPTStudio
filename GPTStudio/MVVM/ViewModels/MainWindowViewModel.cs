using GPTStudio.MVVM.Core;
using GPTStudio.MVVM.View.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.MVVM.ViewModels
{
    class MainWindowViewModel : ObservableObject
    {
        public RelayCommand MessengerCommand { get; private set; }
        public RelayCommand HomeCommand { get; private set; }
        public RelayCommand BookmarksCommand { get; private set; }

        #region Properties
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public static MessengerView MessengerV { get; private set; } = new MessengerView();
        #endregion

        public MainWindowViewModel()
        {
            CurrentView = MessengerV;

            MessengerCommand = new RelayCommand(o => CurrentView = MessengerV);
            HomeCommand      = new RelayCommand(o => CurrentView = null);
            BookmarksCommand = new RelayCommand(o => CurrentView = null);
        }
    }
}
