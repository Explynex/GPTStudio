using GPTStudio.MVVM.Core;
using GPTStudio.MVVM.View.Controls;
using System.Windows;

namespace GPTStudio.MVVM.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    public RelayCommand MessengerCommand { get; private set; }
    public RelayCommand HomeCommand { get; private set; }
    public RelayCommand BookmarksCommand { get; private set; }
    public RelayCommand SettingsCommand { get; private set; }
    public RelayCommand ClosePopupCommand { get; private set; }

    #region Properties
    private object _currentView;
    public object CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private object _popupContent;
    public object PopupContent
    {
        get => _popupContent;
        set => SetProperty(ref _popupContent, value);
    }

    public static MessengerView MessengerV { get; private set; } = new MessengerView();
    public static SettingsView SettingsV { get; private set; }
    #endregion

    private bool _isPopupActive;
    public bool IsPopupActive 
    {
        get => _isPopupActive;
        set => SetProperty(ref _isPopupActive, value); 
    }


    public MainWindowViewModel()
    {
        CurrentView = MessengerV;

        MessengerCommand = new (o => CurrentView = MessengerV);
        HomeCommand      = new (o => CurrentView = null);
        BookmarksCommand = new (o => CurrentView = null);
        SettingsCommand = new(o =>
        {
            PopupContent = SettingsV ??= new SettingsView();
            if (IsPopupActive)
            {
                IsPopupActive = false;
                return;
            }
            IsPopupActive = true;
        });

        ClosePopupCommand = new(o =>
        {
            (o as SettingsView).Visibility = Visibility.Collapsed;
            IsPopupActive = false;
        });
    }
}
