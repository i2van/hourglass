using System.Diagnostics;
using System.IO;

// ReSharper disable ExceptionNotDocumented

namespace Hourglass.Extensions;

internal static class ShellExtensions
{
    /// <summary>
    /// Opens Windows Explorer and selects the specified file, or opens its parent directory if the file does not
    /// exist.
    /// </summary>
    /// <param name="filePath">The path to the file to show in Explorer.</param>
    public static void OpenFileLocation(string filePath)
    {
        string explorerArgs = File.Exists(filePath)
            ? $"/select,\"{filePath}\""
            : Path.GetDirectoryName(filePath) is { } dir
                ? $"\"{dir}\""
                : filePath;
        Process.Start("explorer.exe", explorerArgs);
    }
}
