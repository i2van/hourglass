using System;
using System.IO;

using Hourglass.Extensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hourglass.Test;

/// <summary>
/// Contains tests for the <see cref="PathExtensions"/> class.
/// </summary>
[TestClass]
public sealed class PathExtensionsTest
{
    private string _baseDirectory = null!;
    private string _appPath = null!;

    /// <summary>
    /// Creates the base directory with the app file.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _appPath = Path.Combine(_baseDirectory, "Tools", "app.cmd");

        Directory.CreateDirectory(Path.GetDirectoryName(_appPath)!);
        File.WriteAllText(_appPath, string.Empty);
    }

    /// <summary>
    /// Deletes the base directory.
    /// </summary>
    [TestCleanup]
    public void Cleanup() =>
        Directory.Delete(_baseDirectory, true);

    /// <summary>
    /// Tests that <see cref="PathExtensions.GetAppFullPath(string, string, bool)"/> resolves the existing app path relative
    /// to the base directory, or as is if it is absolute.
    /// </summary>
    /// <param name="path">The app path.</param>
    [TestMethod]
    [DataRow(@"Tools\app.cmd")]
    [DataRow(@" ""Tools\app.cmd"" ")]
    [DataRow(@".\Tools\..\Tools\app.cmd")]
    public void GetAppFullPathResolvesRelativePath(string path)
    {
        // Act & Assert
        Assert.AreEqual(_appPath, PathExtensions.GetAppFullPath(path, _baseDirectory));
        Assert.AreEqual(_appPath, PathExtensions.GetAppFullPath(_appPath, Path.GetTempPath()));
    }

    /// <summary>
    /// Tests that <see cref="PathExtensions.GetAppFullPath(string, string, bool)"/> expands the environment variables.
    /// </summary>
    [TestMethod]
    public void GetAppFullPathExpandsEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("HOURGLASS_TEST_DIR", _baseDirectory);

        try
        {
            // Act & Assert
            Assert.AreEqual(_appPath, PathExtensions.GetAppFullPath(@"%HOURGLASS_TEST_DIR%\Tools\app.cmd", Path.GetTempPath()));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOURGLASS_TEST_DIR", null);
        }
    }

    /// <summary>
    /// Tests that <see cref="PathExtensions.GetAppFullPath(string, string, bool)"/> returns <see langword="null"/> if the
    /// path is empty, invalid or the app does not exist.
    /// </summary>
    [TestMethod]
    public void GetAppFullPathReturnsNullForMissingApp()
    {
        // Act & Assert
        foreach (string? path in new[] { null, string.Empty, "  ", "Tools", @"Tools\missing.cmd", "a|b" })
        {
            Assert.IsNull(PathExtensions.GetAppFullPath(path, _baseDirectory), path ?? "null");
        }
    }

    /// <summary>
    /// Tests that <see cref="PathExtensions.GetAppFullPath(string, string, bool)"/> resolves the path of the missing
    /// app if the app does not have to exist.
    /// </summary>
    [TestMethod]
    public void GetAppFullPathResolvesMissingAppIfNotMustExist()
    {
        // Act & Assert
        Assert.AreEqual(Path.Combine(_baseDirectory, "Tools", "missing.cmd"), PathExtensions.GetAppFullPath(@"""Tools\missing.cmd""", _baseDirectory, mustExist: false));
        Assert.IsNull(PathExtensions.GetAppFullPath("  ", _baseDirectory, mustExist: false));
    }

    /// <summary>
    /// Tests that <see cref="PathExtensions.ToAppRelativePath(string, string)"/> returns the path relative to the
    /// base directory if the path is within it, or the path as is otherwise.
    /// </summary>
    [TestMethod]
    public void ToAppRelativePathReturnsRelativePathWithinBaseDirectory()
    {
        // Arrange
        string outsidePath = Path.Combine($"{_baseDirectory}Other", "app.cmd");

        // Act & Assert
        Assert.AreEqual(@"Tools\app.cmd", PathExtensions.ToAppRelativePath(_appPath, _baseDirectory));
        Assert.AreEqual(@"Tools\app.cmd", PathExtensions.ToAppRelativePath($"\"{_appPath}\"", $@"{_baseDirectory.ToUpperInvariant()}\"));
        Assert.AreEqual(outsidePath, PathExtensions.ToAppRelativePath(outsidePath, _baseDirectory));
        Assert.AreEqual(@"Tools\app.cmd", PathExtensions.ToAppRelativePath(@" Tools\app.cmd ", _baseDirectory));
        Assert.AreEqual(string.Empty, PathExtensions.ToAppRelativePath(null, _baseDirectory));
    }
}
