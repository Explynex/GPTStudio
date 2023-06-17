using GPTStudio.TelegramProvider.Infrastructure;
using GPTStudio.TelegramProvider.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GPTStudio.TelegramProvider;
internal partial class App
{
    private const string ProjectRawURL = "https://raw.githubusercontent.com/k1tbyte/GPTStudio/master/GPTStudio.TelegramProvider/GPTStudio.TelegramProvider.csproj";
    private const string ReleaseURL = "https://github.com/k1tbyte/GPTStudio/releases/download/";

    [GeneratedRegex("<AssemblyVersion>(.*?)</AssemblyVersion>")]
    private static partial Regex AssemblyVersionRegex();

    private static async Task CheckUpdate()
    {
        try
        {
            Logger.Print("Checking updates...");
            var response = await HttpClient.GetStringAsync(ProjectRawURL);

            if (string.IsNullOrEmpty(response))
            {
                Logger.Print("Update check failed", color: ConsoleColor.Red);
                return;
            }


            var match = AssemblyVersionRegex().Match(response);
            var fetchedVer = Version.Parse(match.Groups[1].Value);
            if (fetchedVer <= Version)
            {
                Logger.Print("The latest version is already installed.");
                return;
            }


            var updateResponse = await HttpClient.GetAsync($"{ReleaseURL}GPTStudio.TelegramProvider-{OS.GetName()}-{RuntimeInformation.OSArchitecture}.zip");

            if (updateResponse.StatusCode != System.Net.HttpStatusCode.OK)
                return;

            Logger.Print($"A newer version {fetchedVer.ToReadable()} has been found. Installation....", color: ConsoleColor.Green);

            Common.CreateDirIfNotExists($"{WorkingDir}.temp");

            var path = Path.Combine(Environment.CurrentDirectory, ".temp"," ").TrimEnd();
            bool isWin = OperatingSystem.IsWindows();
            using (var fs = new FileStream($"{path}update", FileMode.CreateNew))
            {
                await updateResponse.Content.CopyToAsync(fs);
            }
            

            System.IO.Compression.ZipFile.ExtractToDirectory($"{path}update",path);

            if(!isWin)
                File.SetUnixFileMode(path + Path.GetFileName(Environment.ProcessPath), UnixFileMode.UserWrite | UnixFileMode.UserRead | UnixFileMode.UserExecute);

            Common.ExecConsoleCommand(
                $"{(isWin ? "move /Y" : "mv -f")} \"{path + Path.GetFileName(Environment.ProcessPath)}\" \"{Environment.ProcessPath}\" && " +
                $"{(isWin ? "rmdir /S /Q" : "rm -rf")} \"{path}\" && " +
                $"{Environment.ProcessPath}",3);

            Shutdown();

            return;
        }
        catch(Exception e)
        {
            Logger.Print("An error occurred while installing the update, check log.txt", color: ConsoleColor.Red);
            Logger.PrintError(e.ToString());
            return;
        }
      
    }
}
