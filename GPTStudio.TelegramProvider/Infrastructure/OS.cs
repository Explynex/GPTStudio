using GPTStudio.TelegramProvider.Utils;
using System.Security.Principal;

namespace GPTStudio.TelegramProvider.Infrastructure;
internal static class OS
{
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
