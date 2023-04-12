using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GPTStudio.Infrastructure
{
    internal static class Config
    {
        public static Models.Properties Properties { get; set; }
        private static readonly string Path = App.WorkingDirectory + "\\properties.json";
        public static bool NeedToUpdate { get; set; }

        public static bool Load()
        {
            if(!File.Exists(Path))
            {
                Properties = new();
                NeedToUpdate = true;
                return true;
            }

            try
            {
                Properties = JsonSerializer.Deserialize<Models.Properties>(File.ReadAllText(Path));
                return true;
            }
            catch
            {
                File.Delete(Path);
                Properties = new();
                NeedToUpdate = true;
                return false;
            }
        }

        public static void Save()
        {
            if (Properties == null)
                return;

            File.WriteAllText(Path,JsonSerializer.Serialize<Models.Properties>(Properties));
        }
    }
}
