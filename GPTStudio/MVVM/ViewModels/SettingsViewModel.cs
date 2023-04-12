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
    }
}
