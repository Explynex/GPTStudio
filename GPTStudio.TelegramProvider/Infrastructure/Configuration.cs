using GPTStudio.OpenAI.Tokenizer;
using GPTStudio.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using GPTStudio.TelegramProvider.Utils;

namespace GPTStudio.TelegramProvider.Infrastructure;
internal static partial class Configuration
{
    private class Env
    {
        public string TelegramBotToken { get; set; }
        public string OpenAIApiKey { get; set; }
        public string DatabaseEndpoint { get; set; }
        public long ErrorsRecieverChatId { get; set; }
    }

    public static TelegramBotClient Client { get; private set; }
    public static OpenAIClient GPTClient { get; private set; }
    public static string DatabaseEndpoint { get; private set; }
    public static long ErrorsRecieverChatId { get; private set; }
    public static GPTTokenizer Tokenizer = new(Properties.Resources.TokenizerMerges);

    [GeneratedRegex("sk-([a-zA-Z0-9]{48})+$")]
    private static partial Regex OpenAIApiKey();

    public static void Setup()
    {
        Env cfg = new();

        if (File.Exists("env.json"))
        {
            cfg = JsonSerializer.Deserialize<Env>(File.ReadAllText("env.json"));
        }

        string input = null;

        while (cfg.TelegramBotToken == null)
        {
            Logger.Print("Enter Telegram access token: ", false);
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.Print("Invalid token!");
                continue;
            }

            cfg.TelegramBotToken = input;
            break;
        }

        while (cfg.OpenAIApiKey == null)
        {
            Logger.Print("Enter OpenAI API key: ", false);
            input = Console.ReadLine();
            if (!OpenAIApiKey().IsMatch(input))
            {
                Logger.Print("Invalid API key!");
                continue;
            }

            cfg.OpenAIApiKey = input;
            break;
        }

        while (cfg.DatabaseEndpoint == null)
        {
            Logger.Print("Enter MongoDB connection string: : ", false);
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Logger.Print("Invalid connection string!");
                continue;
            }

            cfg.DatabaseEndpoint = input;
            break;
        }


        if (input != null)
            File.WriteAllText("env.json",JsonSerializer.Serialize(cfg));

        GPTClient        = new(cfg.OpenAIApiKey);
        Client           = new(cfg.TelegramBotToken);
        DatabaseEndpoint = cfg.DatabaseEndpoint;

        cfg = null;
    }
}
