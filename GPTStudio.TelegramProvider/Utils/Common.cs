using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GPTStudio.TelegramProvider.Utils;
internal static partial class Common
{
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
}
