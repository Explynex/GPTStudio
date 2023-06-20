using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.TelegramProvider.Infrastructure;
internal static partial class NativeMethods
{
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("Windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("kernel32.dll")]
    internal static partial bool GetConsoleMode(nint hConsoleHandle, out EConsoleMode lpMode);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("FreeBSD")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("MacOS")]
    [LibraryImport("libc", EntryPoint = "geteuid", SetLastError = true)]
    internal static partial uint GetEuid();

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("Windows")]
    [LibraryImport("kernel32.dll")]
    internal static partial nint GetStdHandle(EStandardHandle nStdHandle);


    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("Windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("kernel32.dll")]
    internal static partial bool SetConsoleMode(nint hConsoleHandle, EConsoleMode dwMode);

    [Flags]
    [SupportedOSPlatform("Windows")]
    internal enum EConsoleMode : uint
    {
        EnableQuickEditMode = 0x0040
    }

    [SupportedOSPlatform("Windows")]
    internal enum EStandardHandle
    {
        Input = -10
    }
}
