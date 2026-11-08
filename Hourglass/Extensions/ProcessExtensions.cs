using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Hourglass.Extensions;

/// <summary>
/// Starts processes.
/// </summary>
public static class ProcessExtensions
{
    private const uint CREATE_NO_WINDOW = 0x08000000;
    private const int STARTF_USESHOWWINDOW = 0x00000001;
    private const short SW_SHOWMINNOACTIVE = 7;

    /// <summary>
    /// Returns the argument quoted so that it is parsed back as is by the <c>CommandLineToArgvW</c> and the C runtime.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <returns>The quoted argument.</returns>
    public static string QuoteArgument(string argument)
    {
        StringBuilder quoted = new("\"");
        int backslashes = 0;

        foreach (char c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            // Backslashes are literal unless they precede a double quote.
            quoted.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes).Append(c);
            backslashes = 0;
        }

        return quoted.Append('\\', backslashes * 2).Append('"').ToString();
    }

    /// <summary>
    /// Starts the app with the argument and does not wait for it to exit. The app directory is used as the working
    /// directory.
    /// </summary>
    /// <param name="fullPath">The app full path.</param>
    /// <param name="argument">The argument.</param>
    /// <param name="silent">A value indicating whether to start a console app without a console window, and a GUI app
    /// minimized and not activated.</param>
    /// <exception cref="Win32Exception">The app could not be started.</exception>
    public static void StartDetached(string fullPath, string argument, bool silent)
    {
        StringBuilder commandLine = new($"\"{fullPath}\" {QuoteArgument(argument)}");

        STARTUPINFO startupInfo = new()
        {
            cb = Marshal.SizeOf<STARTUPINFO>()
        };

        if (silent)
        {
            startupInfo.dwFlags = STARTF_USESHOWWINDOW;
            startupInfo.wShowWindow = SW_SHOWMINNOACTIVE;
        }

        if (!NativeMethods.CreateProcess(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                silent ? CREATE_NO_WINDOW : 0,
                IntPtr.Zero,
                Path.GetDirectoryName(fullPath),
                ref startupInfo,
                out PROCESS_INFORMATION processInformation))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        NativeMethods.CloseHandle(processInformation.hThread);
        NativeMethods.CloseHandle(processInformation.hProcess);
    }
}
