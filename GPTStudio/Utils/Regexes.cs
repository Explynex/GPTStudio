using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GPTStudio.Utils;

internal static partial class Regexes
{
    [GeneratedRegex("[\\w]")]
    public static partial Regex Name();

    [GeneratedRegex("(?<=[\\w]{3,}[.?!])\\s+(?=[\\p{Lu}\\p{Lo}])")]
    public static partial Regex Sentence();

    [GeneratedRegex("[\\~#%&*{}/:<>?|\"-]")]
    public static partial Regex WindowsFileName();

    [GeneratedRegex("sk-([a-zA-Z0-9]{48})+$")]
    public static partial Regex OpenAIApiKey();
}
