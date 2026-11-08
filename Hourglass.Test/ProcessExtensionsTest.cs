using System;
using System.Runtime.InteropServices;

using Hourglass.Extensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hourglass.Test;

/// <summary>
/// Contains tests for the <see cref="ProcessExtensions"/> class.
/// </summary>
[TestClass]
public sealed class ProcessExtensionsTest
{
    /// <summary>
    /// Tests that <see cref="ProcessExtensions.QuoteArgument"/> returns the argument that is parsed back as is by the
    /// <c>CommandLineToArgvW</c>.
    /// </summary>
    /// <param name="argument">The argument.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("a b")]
    [DataRow("a\"b")]
    [DataRow("a\\\"b")]
    [DataRow("a\\b\\")]
    [DataRow("a\\\\")]
    [DataRow("\\\\\"")]
    [DataRow("\"\"")]
    [DataRow("{\"Title\":\"T \\\"q\\\" \\\\\",\"Iteration\":1}")]
    public void QuoteArgumentRoundTrips(string argument)
    {
        // Act
        string[] actual = CommandLineToArgs($"app.exe {ProcessExtensions.QuoteArgument(argument)}");

        // Assert
        CollectionAssert.AreEqual(new[] { "app.exe", argument }, actual);
    }

    private static string[] CommandLineToArgs(string commandLine)
    {
        IntPtr argv = CommandLineToArgv(commandLine, out int argc);

        try
        {
            string[] args = new string[argc];

            for (int i = 0; i < argc; i++)
            {
                args[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size))!;
            }

            return args;
        }
        finally
        {
            LocalFree(argv);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CommandLineToArgv(string lpCmdLine, out int pNumArgs);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
