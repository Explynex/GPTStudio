using GPTStudio.TelegramProvider.Utils;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace GPTStudio.TelegramProvider.Infrastructure;
internal static class OS
{
    internal static readonly string ProcessFileName = Environment.ProcessPath ?? throw new InvalidOperationException(nameof(ProcessFileName));
    private static Mutex? SingleInstance;

    internal static async Task<bool> RegisterProcess()
    {
        if (SingleInstance != null)
        {
            return false;
        }

        string uniqueName = $"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Directory.GetCurrentDirectory())))}";

        Mutex? singleInstance = null;

        for (byte i = 0; i < 5; i++)
        {
            if (i > 0)
            {
                await Task.Delay(2000).ConfigureAwait(false);
            }

            singleInstance = new Mutex(true, uniqueName, out bool result);

            if (result)
                break;


            singleInstance.Dispose();
            singleInstance = null;
        }

        if (singleInstance == null)
        {
            return false;
        }

        SingleInstance = singleInstance;

        return true;
    }

    internal static void UnregisterProcess()
    {
        if (SingleInstance == null)
        {
            return;
        }

        SingleInstance.Dispose();
        SingleInstance = null;
    }

    [SupportedOSPlatform("Windows")]
    internal static void WindowsDisableQuickEditMode()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        nint consoleHandle = NativeMethods.GetStdHandle(NativeMethods.EStandardHandle.Input);

        if (!NativeMethods.GetConsoleMode(consoleHandle, out NativeMethods.EConsoleMode consoleMode))
        {
            return;
        }

        consoleMode &= ~NativeMethods.EConsoleMode.EnableQuickEditMode;

    }

    internal static bool IsRunningAsRoot()
    {
        if (OperatingSystem.IsWindows())
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            return identity.IsSystem || new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return NativeMethods.GetEuid() == 0;
        }
        return false;
    }

    internal static string? GetName()
    {
        if (OperatingSystem.IsWindows())  return "win";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "osx";
        return null;
    }
}
