using System;
using System.IO;

namespace Hourglass.Extensions;

/// <summary>
/// Resolves the paths of the apps relative to the Hourglass executable directory.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    /// Returns the full path of the app file. Environment variables are expanded, and a relative path is resolved
    /// relative to the Hourglass executable directory.
    /// </summary>
    /// <param name="path">The absolute or relative app path.</param>
    /// <param name="mustExist">A value indicating whether the app file must exist.</param>
    /// <returns>The full path of the app file, or <see langword="null"/> if <paramref name="path"/> is empty,
    /// invalid or <paramref name="mustExist"/> is <see langword="true"/> and the file does not exist.</returns>
    public static string? GetAppFullPath(string? path, bool mustExist = true) =>
        GetAppFullPath(path, AssemblyExtensions.GetExecutableDirectoryName(), mustExist);

    /// <summary>
    /// Returns the full path of the app file. Environment variables are expanded, and a relative path is resolved
    /// relative to the <paramref name="baseDirectory"/>.
    /// </summary>
    /// <param name="path">The absolute or relative app path.</param>
    /// <param name="baseDirectory">The directory a relative path is relative to.</param>
    /// <param name="mustExist">A value indicating whether the app file must exist.</param>
    /// <returns>The full path of the app file, or <see langword="null"/> if <paramref name="path"/> is empty,
    /// invalid or <paramref name="mustExist"/> is <see langword="true"/> and the file does not exist.</returns>
    public static string? GetAppFullPath(string? path, string baseDirectory, bool mustExist = true)
    {
        string trimmedPath = Trim(path);

        if (trimmedPath.Length == 0)
        {
            return null;
        }

        try
        {
            string fullPath = Path.GetFullPath(Path.Combine(baseDirectory, Environment.ExpandEnvironmentVariables(trimmedPath)));

            return !mustExist || File.Exists(fullPath) ? fullPath : null;
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the path relative to the Hourglass executable directory if the path is within it, or the path as is
    /// otherwise.
    /// </summary>
    /// <param name="path">The app path.</param>
    /// <returns>The path relative to the Hourglass executable directory if the path is within it, or the path as is
    /// otherwise.</returns>
    public static string ToAppRelativePath(string? path) =>
        ToAppRelativePath(path, AssemblyExtensions.GetExecutableDirectoryName());

    /// <summary>
    /// Returns the path relative to the <paramref name="baseDirectory"/> if the path is within it, or the path as is
    /// otherwise.
    /// </summary>
    /// <param name="path">The app path.</param>
    /// <param name="baseDirectory">The directory the path is made relative to.</param>
    /// <returns>The path relative to the <paramref name="baseDirectory"/> if the path is within it, or the path as is
    /// otherwise.</returns>
    public static string ToAppRelativePath(string? path, string baseDirectory)
    {
        string trimmedPath = Trim(path);

        try
        {
            if (!Path.IsPathRooted(trimmedPath))
            {
                return trimmedPath;
            }

            string fullPath = Path.GetFullPath(trimmedPath);
            string fullBaseDirectory = $"{Path.GetFullPath(baseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}";

            return fullPath.StartsWith(fullBaseDirectory, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(fullBaseDirectory.Length)
                : fullPath;
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            return trimmedPath;
        }
    }

    private static string Trim(string? path) =>
        (path ?? string.Empty).Trim().Trim('"').Trim();
}
