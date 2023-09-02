using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GPTStudio.TelegramProvider.Utils;
internal static partial class NativeMethods
{
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("FreeBSD")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("MacOS")]
    [LibraryImport("libc", EntryPoint = "geteuid", SetLastError = true)]
    internal static partial uint GetEuid();
}
