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

    [GeneratedRegex("(?<=[.?!])\\s+(?=[\\p{Lu}\\p{Lo}])")]
    public static partial Regex Sentence();
}
