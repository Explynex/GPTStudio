using GPTStudio.Infrastructure;
using GPTStudio.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.MVVM.ViewModels
{
    class SettingsViewModel : ObservableObject
    {
        public static Infrastructure.Models.Properties Properties => Config.Properties;
        public bool UsingMarkdown
        {
            get => Config.Properties.UsingMarkdown;
            set
            {
                (MainWindowViewModel.MessengerV.DataContext as MessengerViewModel).UsingMarkdown = Config.Properties.UsingMarkdown = value;
            }
        }
    }
}
