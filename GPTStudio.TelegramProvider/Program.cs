using GPTStudio.TelegramProvider.Commands;
using GPTStudio.TelegramProvider.Common;
using GPTStudio.TelegramProvider.Core;
using GPTStudio.TelegramProvider.Infrastructure;
using System.Runtime.InteropServices;

namespace GPTStudio.TelegramProvider;


internal partial class Program
{



    public static bool IsShuttingDown { get; private set; }         = false;
    public static HttpClient HttpClient { get; private set; }       = new();


    private static readonly TaskCompletionSource<byte> ShutdownResetEvent = new();

    static async Task Main(string[] args)
    {
        Console.Clear();
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => OnUnhandledException(e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (sender,e) => OnUnhandledException(e.Exception);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("   ___   ___   _____   ___   _               _   _       \n" +
            "  / __| | _ \\ |_   _| / __| | |_   _  _   __| | (_)  ___ \n" + " | (_ | |  _/   | |   \\__ \\ |  _| | || | / _` | | | / _ \\\n"
            + "  \\___| |_|     |_|   |___/  \\__|  \\_,_| \\__,_| |_| \\___/\n_________________________________________________________________\n");
        Console.ForegroundColor = ConsoleColor.White;

        if (OS.IsRunningAsRoot())
        {
            Logger.Print("You're attempting to run GPTStudio as the administrator (root). GPTStudio does not require root access for its operation, we recommend to run it as non-administrator user if possible. Continue? [y\\n]: ", false, color: ConsoleColor.Gray);
            var key = Console.ReadLine();
            if (key?[0] != 'y')
                return;
        }

        Logger.Print($"Starting {SharedInfo.Version.ToReadable()}",color: ConsoleColor.DarkYellow);
        await CheckUpdate();

        await App.Init();

        if (args.Length > 0 && args[0].ToLower() == "--console")
        {
            while (!IsShuttingDown)
            {
                Logger.Print("Command: /", false, color: ConsoleColor.Cyan);
                var cmd = Console.ReadLine();
                if (!string.IsNullOrEmpty(cmd))
                    ConsoleHandler.HandleConsoleCommand(cmd);
            }
        }
        else
           await ShutdownResetEvent.Task.ConfigureAwait(false);


        static void OnUnhandledException(object e)
        {
            Logger.PrintError(e.ToString()!);
            Shutdown();
        }
    }

    public static void Shutdown()
    {
        ShutdownResetEvent.TrySetResult(0);
        Environment.Exit(0);
    }

    public static void Restart()
    {
        Utils.ExecConsoleCommand($"\"{Environment.ProcessPath}\"", 3);
        Shutdown();
    }


    private static async Task CheckUpdate()
    {
        try
        {
            Logger.Print("Checking updates...");
            var response = await HttpClient.GetStringAsync(SharedInfo.ProjectRawURL);

            if (string.IsNullOrEmpty(response))
            {
                Logger.Print("Update check failed", color: ConsoleColor.Red);
                return;
            }


            var match = GeneratedRegexes.AssemblyVersion().Match(response);
            var fetchedVer = Version.Parse(match.Groups[1].Value);
            if (fetchedVer <= SharedInfo.Version)
            {
                Logger.Print("The latest version is already installed.");
                return;
            }


            Logger.Print($"A newer version {fetchedVer.ToReadable()} has been found. Installation....", color: ConsoleColor.Green);

            var updateResponse = await HttpClient.GetAsync($"{SharedInfo.GithubReleases}/{fetchedVer}/GPTStudio.TelegramProvider-{OS.GetName()}-{RuntimeInformation.OSArchitecture.ToString().ToLower()}.zip");

            if (updateResponse.StatusCode != System.Net.HttpStatusCode.OK)
                return;

            Utils.CreateDirIfNotExists($"{SharedInfo.WorkingDir}.temp");

            var path = Path.Combine(Environment.CurrentDirectory, ".temp", " ").TrimEnd();
            bool isWin = OperatingSystem.IsWindows();
            using (var fs = new FileStream($"{path}update", FileMode.CreateNew))
            {
                await updateResponse.Content.CopyToAsync(fs);
            }


            System.IO.Compression.ZipFile.ExtractToDirectory($"{path}update", path);
            File.Delete($"{path}update");


            if (!isWin)
                File.SetUnixFileMode(path + Path.GetFileName(Environment.ProcessPath), UnixFileMode.UserWrite | UnixFileMode.UserRead | UnixFileMode.UserExecute);

            Utils.ExecConsoleCommand(
                $"{(isWin ? "move /Y" : "mv -f")} \"{path}*\" \"{SharedInfo.WorkingDir}\" && " +
                $"{(isWin ? "rmdir /S /Q" : "rm -rf")} \"{path}\" && " +
                $"{Environment.ProcessPath}", 3);

            Shutdown();

            return;
        }
        catch (Exception e)
        {
            Logger.Print("An error occurred while installing the update, check log.txt", color: ConsoleColor.Red);
            Logger.PrintError(e.ToString(), false);
            return;
        }

    }



}
