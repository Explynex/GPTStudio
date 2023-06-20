using GPTStudio.OpenAI;
using GPTStudio.OpenAI.Tokenizer;
using GPTStudio.TelegramProvider.Common;
using GPTStudio.TelegramProvider.Core;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace GPTStudio.TelegramProvider.Infrastructure;
internal sealed class Config
{
    public sealed class AzureProperties
    {
        public string? ComputerVisionKey { get; set; }
        public string? ComputerVisionServiceName { get; set; }
    }

    public string? TelegramBotToken { get; set; }
    public string? OpenAIApiKey { get; set; }
    public string? DatabaseEndpoint { get; set; }
    public long ErrorsRecieverChatId { get; set; }
    public AzureProperties Azure { get; set; } = new();


    private static readonly string ConfigPath = Path.Combine(SharedInfo.WorkingDir, SharedInfo.ConfigFileName);

    public static async Task<Config> Load()
    {
        if (!File.Exists(ConfigPath))
            return new();

        string json;

        try
        {
            json = await File.ReadAllTextAsync(ConfigPath).ConfigureAwait(false);

            if (string.IsNullOrEmpty(json))
            {
                Common.Logger.PrintError("Config content is null");
                return new();
            }

            return JsonConvert.DeserializeObject<Config>(json) ?? new();
        }
        catch (Exception e)
        {
            Logger.PrintError(e.ToString());
            return new();
        }
    }

    public static void Save()
    {
        File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(App.Config));
    }

    public static void CheckDefaultConfigValues()
    {
        string? input = null;

        while (App.Config!.TelegramBotToken == null)
        {
            Logger.Print("Enter Telegram access token: ", false);
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.Print("Invalid token!");
                continue;
            }

            App.Config!.TelegramBotToken = input;
            break;
        }

        while (App.Config!.OpenAIApiKey == null)
        {
            Logger.Print("Enter OpenAI API key: ", false);
            input = Console.ReadLine();
            if (!GeneratedRegexes.OpenAIApiKey().IsMatch(input))
            {
                Logger.Print("Invalid API key!");
                continue;
            }

            App.Config!.OpenAIApiKey = input;
            break;
        }

        while (App.Config!.DatabaseEndpoint == null)
        {
            Logger.Print("Enter MongoDB connection string: ", false);
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.Print("Invalid connection string!");
                continue;
            }

            App.Config!.DatabaseEndpoint = input;
            break;
        }


        if (input != null)
            Save();
    }
}
