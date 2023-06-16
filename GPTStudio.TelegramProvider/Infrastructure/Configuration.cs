using GPTStudio.OpenAI;
using GPTStudio.OpenAI.Tokenizer;
using GPTStudio.TelegramProvider.Utils;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace GPTStudio.TelegramProvider.Infrastructure;
internal static partial class Configuration
{
    public class Azure
    {
        public string? ComputerVisionKey { get; set; }
        public string? ComputerVisionServiceName { get; set; }
    }
    public class ConfigProperties
    {
        public string? TelegramBotToken { get; set; }
        public string? OpenAIApiKey { get; set; }
        public string? DatabaseEndpoint { get; set; }
        public long ErrorsRecieverChatId { get; set; }
        public Azure Azure { get; set; } = new();

    }

    public static TelegramBotClient Client { get; private set; }
    public static OpenAIClient GPTClient { get; private set; }
    public static ConfigProperties Props { get; private set; }
    public static GPTTokenizer Tokenizer = new(Properties.Resources.TokenizerMerges);

    [GeneratedRegex("sk-([a-zA-Z0-9]{48})+$")]
    private static partial Regex OpenAIApiKey();

    public static void Setup()
    {
        Props = new();

        if (File.Exists("env.json"))
        {
            Props = JsonSerializer.Deserialize<ConfigProperties>(File.ReadAllText("env.json"))!;
        }

        RequestConfigureData();

        GPTClient        = new(Props.OpenAIApiKey!);
        Client           = new(Props.TelegramBotToken!);
    }

    private static void RequestConfigureData()
    {
        string? input = null;

        while (Props.TelegramBotToken == null)
        {
            Logger.Print("Enter Telegram access token: ", false);
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.Print("Invalid token!");
                continue;
            }

            Props.TelegramBotToken = input;
            break;
        }

        while (Props.OpenAIApiKey == null)
        {
            Logger.Print("Enter OpenAI API key: ", false);
            input = Console.ReadLine();
            if (!OpenAIApiKey().IsMatch(input))
            {
                Logger.Print("Invalid API key!");
                continue;
            }

            Props.OpenAIApiKey = input;
            break;
        }

        while (Props.DatabaseEndpoint == null)
        {
            Logger.Print("Enter MongoDB connection string: ", false);
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.Print("Invalid connection string!");
                continue;
            }

            Props.DatabaseEndpoint = input;
            break;
        }


        if (input != null)
            Save();
    }

    public static void Save()
    {
        File.WriteAllText("env.json", JsonSerializer.Serialize(Props));
    }
}
