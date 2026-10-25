using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Hourglass.Parsing;
using Hourglass.Properties;
using Hourglass.Timing;

namespace Hourglass.Serialization;

/// <summary>
/// Creates the timers report in the <see href="https://www.rfc-editor.org/rfc/rfc4180">RFC 4180</see> CSV format.
/// </summary>
public static class TimerReport
{
    /// <summary>
    /// The timers report file extension.
    /// </summary>
    public const string FileExtension = ".csv";

    private const string FieldSeparator = ",";
    private const string LineBreak = "\r\n";
    private const string Quote = "\"";
    private const string EscapedQuote = "\"\"";
    private const string Yes = "Yes";
    private const string No = "No";

    // ISO 8601 extended format local time without time zone and fraction of a second.
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss";

    // ISO 8601 basic format local time without time zone, since colons are not allowed in file names.
    private const string FileNameDateTimeFormat = "yyyyMMdd'T'HHmmss";

    private static readonly char[] CharsRequiringQuotes = [',', '"', '\r', '\n'];

    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// The timer to report.
    /// </summary>
    /// <param name="TimerInfo">The timer snapshot.</param>
    /// <param name="Status">The timer status as shown in the app.</param>
    public sealed record Entry(TimerInfo TimerInfo, string Status)
    {
        /// <summary>
        /// Returns an <see cref="Entry"/> for the specified <see cref="Timer"/>.
        /// </summary>
        /// <param name="timer">A <see cref="Timer"/>.</param>
        /// <returns>An <see cref="Entry"/> for the specified <see cref="Timer"/>.</returns>
        public static Entry FromTimer(Timer timer) =>
            new(timer.ToTimerInfo(), timer.ToString());
    }

    private sealed record Column(string Name, Func<Entry, object?> GetValue);

    private static readonly Column[] Columns =
    [
        new(nameof(TimerOptionsInfo.Title), static e => e.TimerInfo.Options?.Title),
        new(nameof(Entry.Status), static e => e.Status),
        new(nameof(TimerInfo.State), static e => e.TimerInfo.State),
        new(nameof(TimerInfo.StartTime), static e => e.TimerInfo.StartTime),
        new(nameof(TimerInfo.EndTime), static e => e.TimerInfo.EndTime),
        new(nameof(TimerInfo.TimeElapsed), static e => e.TimerInfo.TimeElapsed),
        new(nameof(TimerInfo.TimeLeft), static e => e.TimerInfo.TimeLeft),
        new(nameof(TimerInfo.TotalTime), static e => e.TimerInfo.TotalTime),
        new(nameof(TimerInfo.TimeExpired), static e => e.TimerInfo.TimeExpired),
        new(nameof(TimerInfo.TimerStart), static e => e.TimerInfo.TimerStart?.TimerStartToken),
        new($"{nameof(TimerInfo.TimerStart)}{nameof(TimerStartToken.OriginalInput)}", static e => e.TimerInfo.TimerStart?.TimerStartToken?.OriginalInput),
        new($"{nameof(TimerInfo.TimerStart)}{nameof(TimerStartInfo.Title)}", static e => e.TimerInfo.TimerStart?.Title),
        new(nameof(TimerInfo.Iteration), static e => e.TimerInfo.Iteration),
        new(nameof(TimerOptionsInfo.AlwaysOnTop), static e => e.TimerInfo.Options?.AlwaysOnTop),
        new(nameof(TimerOptionsInfo.PromptOnExit), static e => e.TimerInfo.Options?.PromptOnExit),
        new(nameof(TimerOptionsInfo.ShowProgressInTaskbar), static e => e.TimerInfo.Options?.ShowProgressInTaskbar),
        new(nameof(TimerOptionsInfo.DoNotKeepComputerAwake), static e => e.TimerInfo.Options?.DoNotKeepComputerAwake),
        new(nameof(TimerOptionsInfo.ReverseProgressBar), static e => e.TimerInfo.Options?.ReverseProgressBar),
        new(nameof(TimerOptionsInfo.DigitalClockTime), static e => e.TimerInfo.Options?.DigitalClockTime),
        new(nameof(TimerOptionsInfo.ShowTimeElapsed), static e => e.TimerInfo.Options?.ShowTimeElapsed),
        new(nameof(TimerOptionsInfo.ShowTriggerTime), static e => e.TimerInfo.Options?.ShowTriggerTime),
        new(nameof(TimerOptionsInfo.ShowIteration), static e => e.TimerInfo.Options?.ShowIteration),
        new(nameof(TimerOptionsInfo.ResetExpiredTimer), static e => e.TimerInfo.Options?.ResetExpiredTimer),
        new(nameof(TimerOptionsInfo.LoopTimer), static e => e.TimerInfo.Options?.LoopTimer),
        new(nameof(TimerOptionsInfo.PauseBeforeLoopTimer), static e => e.TimerInfo.Options?.PauseBeforeLoopTimer),
        new(nameof(TimerOptionsInfo.StartPaused), static e => e.TimerInfo.Options?.StartPaused),
        new(nameof(TimerOptionsInfo.PopUpWhenExpired), static e => e.TimerInfo.Options?.PopUpWhenExpired),
        new(nameof(TimerOptionsInfo.CloseWhenExpired), static e => e.TimerInfo.Options?.CloseWhenExpired),
        new(nameof(TimerOptionsInfo.MinimizeWhenExpired), static e => e.TimerInfo.Options?.MinimizeWhenExpired),
        new(nameof(TimerOptionsInfo.ShutDownWhenExpired), static e => e.TimerInfo.Options?.ShutDownWhenExpired),
        new(nameof(TimerOptionsInfo.ThemeIdentifier), static e => e.TimerInfo.Options?.ThemeIdentifier),
        new(nameof(TimerOptionsInfo.SoundIdentifier), static e => e.TimerInfo.Options?.SoundIdentifier),
        new(nameof(TimerOptionsInfo.LoopSound), static e => e.TimerInfo.Options?.LoopSound),
        new(nameof(TimerOptionsInfo.WindowTitleMode), static e => e.TimerInfo.Options?.WindowTitleMode),
        new(nameof(TimerOptionsInfo.HideWindowTitleBar), static e => e.TimerInfo.Options?.HideWindowTitleBar),
        new(nameof(TimerOptionsInfo.Opacity), static e => e.TimerInfo.Options?.Opacity),
        new($"{nameof(WindowSizeInfo.RestoreBounds)}{nameof(Rect.X)}", static e => e.TimerInfo.Options?.WindowSize?.RestoreBounds.X),
        new($"{nameof(WindowSizeInfo.RestoreBounds)}{nameof(Rect.Y)}", static e => e.TimerInfo.Options?.WindowSize?.RestoreBounds.Y),
        new($"{nameof(WindowSizeInfo.RestoreBounds)}{nameof(Rect.Width)}", static e => e.TimerInfo.Options?.WindowSize?.RestoreBounds.Width),
        new($"{nameof(WindowSizeInfo.RestoreBounds)}{nameof(Rect.Height)}", static e => e.TimerInfo.Options?.WindowSize?.RestoreBounds.Height),
        new(nameof(WindowSizeInfo.WindowState), static e => e.TimerInfo.Options?.WindowSize?.WindowState),
        new(nameof(WindowSizeInfo.RestoreWindowState), static e => e.TimerInfo.Options?.WindowSize?.RestoreWindowState),
        new(nameof(WindowSizeInfo.IsFullScreen), static e => e.TimerInfo.Options?.WindowSize?.IsFullScreen),
        new(nameof(TimerOptionsInfo.LockInterface), static e => e.TimerInfo.Options?.LockInterface)
    ];

    /// <summary>
    /// Returns the default timers report file name without the extension, e.g. <c>Hourglass-20261025T164051</c>.
    /// </summary>
    /// <param name="time">The local time.</param>
    /// <returns>The default timers report file name without the extension.</returns>
    public static string GetDefaultFileName(DateTime time) =>
        $"{Resources.AppName}-{time.ToString(FileNameDateTimeFormat, CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Saves the timers report to the file as UTF-8 without the byte order mark.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="entries">The timers.</param>
    public static void Save(string path, IEnumerable<Entry> entries) =>
        File.WriteAllText(path, ToCsv(entries), Utf8WithoutBom);

    /// <summary>
    /// Returns the timers report: the header record followed by the record for each timer, where each record ends
    /// with CRLF and has the same number of fields.
    /// </summary>
    /// <param name="entries">The timers.</param>
    /// <returns>The timers report.</returns>
    public static string ToCsv(IEnumerable<Entry> entries)
    {
        StringBuilder csv = new();

        AppendRecord(csv, Columns.Select(static column => column.Name));

        foreach (Entry entry in entries)
        {
            AppendRecord(csv, Columns.Select(column => Format(column.GetValue(entry))));
        }

        return csv.ToString();
    }

    /// <summary>
    /// Returns the field enclosed in double quotes with each double quote doubled if the field contains a comma,
    /// double quote, CR or LF, or the field as is otherwise.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The escaped field, or an empty string if <paramref name="field"/> is <see langword="null"/>.</returns>
    public static string EscapeField(string? field) =>
        field is null || field.IndexOfAny(CharsRequiringQuotes) < 0
            ? field ?? string.Empty
            : $"{Quote}{field.Replace(Quote, EscapedQuote)}{Quote}";

    private static void AppendRecord(StringBuilder csv, IEnumerable<string?> fields) =>
        csv.Append(string.Join(FieldSeparator, fields.Select(EscapeField))).Append(LineBreak);

    private static string? Format(object? value) => value switch
    {
        null => null,
        bool flag => flag ? Yes : No,
        DateTime dateTime => dateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
        TimeSpan timeSpan => TruncateToSeconds(timeSpan).ToString("c", CultureInfo.InvariantCulture),
        double number => double.IsNaN(number) || double.IsInfinity(number)
            ? null
            : number.ToString("R", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static TimeSpan TruncateToSeconds(TimeSpan timeSpan) =>
        TimeSpan.FromTicks(timeSpan.Ticks - timeSpan.Ticks % TimeSpan.TicksPerSecond);
}
