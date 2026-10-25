using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Hourglass.Serialization;
using Hourglass.Timing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hourglass.Test;

/// <summary>
/// Contains tests for the <see cref="TimerReport"/> class.
/// </summary>
[TestClass]
public sealed class TimerReportTest
{
    #region EscapeField

    /// <summary>
    /// Tests that <see cref="TimerReport.EscapeField"/> returns the field as is when the field does not contain a
    /// comma, double quote, CR or LF.
    /// </summary>
    [TestMethod]
    public void EscapeFieldWithoutSpecialCharactersReturnsFieldAsIs()
    {
        // Act
        string actual = TimerReport.EscapeField("My title 5 minutes");

        // Assert
        Assert.AreEqual("My title 5 minutes", actual);
    }

    /// <summary>
    /// Tests that <see cref="TimerReport.EscapeField"/> returns an empty string for a <see langword="null"/> field.
    /// </summary>
    [TestMethod]
    public void EscapeFieldWithNullReturnsEmptyString()
    {
        // Act
        string actual = TimerReport.EscapeField(null);

        // Assert
        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Tests that <see cref="TimerReport.EscapeField"/> encloses the field in double quotes and doubles each double
    /// quote when the field contains a comma, double quote, CR or LF.
    /// </summary>
    [TestMethod]
    public void EscapeFieldWithSpecialCharactersReturnsQuotedField()
    {
        // Act & Assert
        Assert.AreEqual("\"a,b\"", TimerReport.EscapeField("a,b"));
        Assert.AreEqual("\"a \"\"b\"\" c\"", TimerReport.EscapeField("a \"b\" c"));
        Assert.AreEqual("\"a\rb\"", TimerReport.EscapeField("a\rb"));
        Assert.AreEqual("\"a\nb\"", TimerReport.EscapeField("a\nb"));
    }

    #endregion

    #region GetDefaultFileName

    /// <summary>
    /// Tests that <see cref="TimerReport.GetDefaultFileName"/> returns the app name followed by the local time in the
    /// ISO 8601 basic format without time zone.
    /// </summary>
    [TestMethod]
    public void GetDefaultFileNameReturnsAppNameWithLocalTime()
    {
        // Arrange
        DateTime time = new(2024, 3, 13, 10, 5, 9, DateTimeKind.Local);

        // Act
        string actual = TimerReport.GetDefaultFileName(time);

        // Assert
        Assert.AreEqual("Hourglass-20240313T100509", actual);
    }

    #endregion

    #region ToCsv

    /// <summary>
    /// Tests that <see cref="TimerReport.ToCsv"/> returns the header record followed by the record for each timer,
    /// where each record ends with CRLF, has the same number of fields and the field values round-trip.
    /// </summary>
    [TestMethod]
    public void ToCsvReturnsHeaderAndRecordForEachTimer()
    {
        // Arrange
        const string title = "Tea, \"green\"\r\nhot";
        TimerReport.Entry[] entries =
        [
            new(new TimerInfo
            {
                State = TimerState.Running,
                StartTime = new DateTime(2024, 3, 13, 10, 0, 0),
                TotalTime = TimeSpan.FromMinutes(5),
                Iteration = 2,
                Options = new TimerOptionsInfo { Title = title, AlwaysOnTop = true, Opacity = 80 }
            }, "5 minutes left"),
            new(new TimerInfo(), "Stopped")
        ];

        // Act
        string actual = TimerReport.ToCsv(entries);

        // Assert
        List<string[]> records = ParseCsv(actual);
        string[] header = records[0];

        Assert.AreEqual(3, records.Count);
        Assert.IsTrue(records.All(record => record.Length == header.Length));
        Assert.AreEqual(header.Length, header.Distinct().Count());
        Assert.AreEqual("Title", header[0]);
        Assert.AreEqual("Status", header[1]);

        Assert.AreEqual(title, Field(1, "Title"));
        Assert.AreEqual("5 minutes left", Field(1, "Status"));
        Assert.AreEqual("Running", Field(1, "State"));
        Assert.AreEqual("2024-03-13T10:00:00", Field(1, "StartTime"));
        Assert.AreEqual(string.Empty, Field(1, "EndTime"));
        Assert.AreEqual("00:05:00", Field(1, "TotalTime"));
        Assert.AreEqual("2", Field(1, "Iteration"));
        Assert.AreEqual("Yes", Field(1, "AlwaysOnTop"));
        Assert.AreEqual("No", Field(1, "LoopTimer"));
        Assert.AreEqual("80", Field(1, "Opacity"));
        Assert.AreEqual(string.Empty, Field(1, "RestoreBoundsX"));

        Assert.AreEqual(string.Empty, Field(2, "Title"));
        Assert.AreEqual("Stopped", Field(2, "Status"));
        Assert.AreEqual("Stopped", Field(2, "State"));
        Assert.AreEqual("1", Field(2, "Iteration"));
        Assert.AreEqual(string.Empty, Field(2, "AlwaysOnTop"));

        string Field(int record, string name) =>
            records[record][Array.IndexOf(header, name)];
    }

    /// <summary>
    /// Tests that <see cref="TimerReport.ToCsv"/> removes the fraction of a second from the times and time spans
    /// without rounding.
    /// </summary>
    [TestMethod]
    public void ToCsvRemovesFractionOfSecondFromTimes()
    {
        // Arrange
        TimeSpan almostOneSecond = TimeSpan.FromTicks(TimeSpan.TicksPerSecond - 1);
        TimerReport.Entry[] entries =
        [
            new(new TimerInfo
            {
                StartTime = new DateTime(2024, 3, 13, 23, 59, 59) + almostOneSecond,
                TimeElapsed = almostOneSecond,
                TimeLeft = new TimeSpan(1, 2, 3, 4) + almostOneSecond,
                TotalTime = (TimeSpan.FromSeconds(1) + almostOneSecond).Negate()
            }, string.Empty)
        ];

        // Act
        List<string[]> records = ParseCsv(TimerReport.ToCsv(entries));

        // Assert
        string[] header = records[0];

        Assert.AreEqual("2024-03-13T23:59:59", Field("StartTime"));
        Assert.AreEqual("00:00:00", Field("TimeElapsed"));
        Assert.AreEqual("1.02:03:04", Field("TimeLeft"));
        Assert.AreEqual("-00:00:01", Field("TotalTime"));

        string Field(string name) =>
            records[1][Array.IndexOf(header, name)];
    }

    #endregion

    #region Save

    /// <summary>
    /// Tests that <see cref="TimerReport.Save"/> saves the timers report as UTF-8 without the byte order mark.
    /// </summary>
    [TestMethod]
    public void SaveWritesUtf8WithoutByteOrderMark()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{TimerReport.FileExtension}");
        TimerReport.Entry[] entries = [new(new TimerInfo(), "\u23F9 \u00B7 \u00C9t\u00E9")];

        try
        {
            // Act
            TimerReport.Save(path, entries);

            // Assert
            byte[] bytes = File.ReadAllBytes(path);

            Assert.IsFalse(bytes.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble()));
            Assert.AreEqual(TimerReport.ToCsv(entries), new UTF8Encoding(false, true).GetString(bytes));
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    /// <summary>
    /// Parses the CSV strictly according to RFC 4180: each record must end with CRLF, and each field containing a
    /// comma, double quote, CR or LF must be enclosed in double quotes with each double quote doubled.
    /// </summary>
    /// <param name="csv">The CSV.</param>
    /// <returns>The records.</returns>
    private static List<string[]> ParseCsv(string csv)
    {
        List<string[]> records = [];
        List<string> fields = [];
        StringBuilder field = new();
        int i = 0;

        while (i < csv.Length)
        {
            if (csv[i] == '"')
            {
                for (i++; ; i++)
                {
                    Assert.IsTrue(i < csv.Length, "Unterminated escaped field.");

                    if (csv[i] != '"')
                    {
                        field.Append(csv[i]);
                    }
                    else if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        i++;
                        break;
                    }
                }
            }
            else
            {
                while (i < csv.Length && csv[i] is not (',' or '"' or '\r' or '\n'))
                {
                    field.Append(csv[i++]);
                }
            }

            fields.Add(field.ToString());
            field.Clear();

            if (i < csv.Length && csv[i] == ',')
            {
                i++;
                continue;
            }

            Assert.IsTrue(i + 1 < csv.Length && csv[i] == '\r' && csv[i + 1] == '\n', $"CRLF expected at {i}.");

            i += 2;
            records.Add([..fields]);
            fields.Clear();
        }

        return records;
    }
}
