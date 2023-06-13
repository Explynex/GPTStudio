using GPTStudio.OpenAI.Chat;
using GPTStudio.OpenAI.Models;
using GPTStudio.TelegramProvider.Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Utils;
internal static partial class Common
{

    /// <summary>
    /// If you specify a user message, the chat context will not be taken into account
    /// </summary>
    /// <param name="chat"></param>
    /// <param name="user"></param>
    /// <param name="userMessage"></param>
    /// <returns></returns>
    public static ChatRequest GenerateChatRequest(GChat chat,GUser user, GChatMessage? userMessage = null)
    {
        int totalTokens = 0;
        List<GChatMessage> msgList = new();


        if (!string.IsNullOrEmpty(user.ChatMode.SystemMessage))
        {
            totalTokens = Env.Tokenizer.Calculate(user.ChatMode.SystemMessage);
            msgList.Add(new GChatMessage(0, user.ChatMode.SystemMessage, null) { Role = Role.System });
        }

        if(userMessage == null)
        {
            for (int i = chat.Messages.Count - 1, insertIndex = totalTokens == 0 ? 0 : 1; i >= 0; i--)
            {
                var gMsg = chat.Messages[i];

                if (gMsg.MessageType != GMessageType.Text)
                    continue;
                if ((totalTokens + gMsg.Tokens) > 3000)
                    break;

                totalTokens += gMsg.Tokens;
                msgList.Insert(insertIndex, chat.Messages[i]);
            }
        }
        else msgList.Add(userMessage);

        return new ChatRequest(msgList, Model.GPT3_5_Turbo, maxTokens: user.ChatMode.MaxTokens);
    }

    public static Stream StreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    [GeneratedRegex("^(-\\d|\\d+){1,9}$")]
    public static partial Regex Integer();

    public static void SetPropertyValue(this object obj, string propName, object value)
    {
        obj.GetType().GetProperty(propName).SetValue(obj, value, null);
    }

    public static object GetPropertyValue(this object obj, string propName)
    {
        return obj.GetType().GetProperty(propName).GetValue(obj, null);
    }

    public static string[] SplitCamelCase(string input)
    {
        return Regex.Replace(input, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled).Split(' ');
    }

    public static string ToReadableString(this TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0} d, ", span.Days) : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} h, ", span.Hours) : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} m, ", span.Minutes) : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} s", span.Seconds) : string.Empty);

        if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

        if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

        return formatted;
    }

    public static void ExecConsoleCommand(string command,int? sleep = null)
    {
        using var process = new Process();
        process.StartInfo.UseShellExecute = false;
        if (OperatingSystem.IsWindows())
        {
            process.StartInfo.FileName = "cmd";
            process.StartInfo.Arguments = $"/c \"{(sleep.HasValue ? $"timeout {sleep} /nobreak > nul &&" : null)} {command}\"";
        }
        else if(OperatingSystem.IsLinux())
        {
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{(sleep.HasValue ? $"sleep {sleep} &&" : null)} {command}\"";
        }
        process.Start();
    }
}
