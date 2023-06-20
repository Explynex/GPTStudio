using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.TelegramProvider;
internal static class SharedInfo
{
    public const string ConfigFileName = "env.json";
    public const string LogFileName    = "log.txt";
    public const string UpdateDir      = ".temp";
    public const string GithubReleases = "https://github.com/k1tbyte/GPTStudio/releases/download/";
    public const string ProjectRawURL  = "https://raw.githubusercontent.com/k1tbyte/GPTStudio/master/GPTStudio.TelegramProvider/GPTStudio.TelegramProvider.csproj";

    /// <summary>
    /// (Major.Minor.Build.Revision)
    /// </summary>
    public static readonly Version Version   = Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException(nameof(Version));
    public static readonly string WorkingDir = Path.Combine(Environment.CurrentDirectory, " ").TrimEnd();
}
