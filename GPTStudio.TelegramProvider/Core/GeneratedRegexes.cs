using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GPTStudio.TelegramProvider.Core;
internal static partial class GeneratedRegexes
{
    [GeneratedRegex("sk-([a-zA-Z0-9]{48})+$")]
    public static partial Regex OpenAIApiKey();

    [GeneratedRegex("^(-\\d|\\d+){1,9}$")]
    public static partial Regex Integer();

    [GeneratedRegex("<AssemblyVersion>(.*?)</AssemblyVersion>")]
    public static partial Regex AssemblyVersion();
}
