using System;
using System.Threading.Tasks;

using Hourglass.Extensions;
using Hourglass.Properties;
using Hourglass.Serialization;
using Hourglass.Timing;

namespace Hourglass.Managers;

/// <summary>
/// Runs the user-selected app on the timer events.
/// </summary>
public static class TimerEventAppManager
{
    /// <summary>
    /// Runs the app configured in the <see cref="Settings.TimerEventAppPath"/> with the timer data in the JSON format
    /// as the command line argument. Does nothing if the app is disabled or does not exist.
    /// </summary>
    /// <remarks>
    /// The JSON is built synchronously to capture the timer state at the event time, while the app path is resolved
    /// and the app is started in the background, so the file system is not accessed on the UI thread. The app
    /// existence is not checked, as the process creation simply fails if the app does not exist.
    /// </remarks>
    /// <param name="eventName">The timer event name.</param>
    /// <param name="timer">The timer.</param>
    public static void Run(string eventName, Timer timer)
    {
        Settings settings = Settings.Default;

        string path = settings.TimerEventAppPath;

        if (settings.TimerEventAppDisabled || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        bool silent = settings.TimerEventAppSilent;

        try
        {
            string json = TimerReport.ToJson(eventName, TimerReport.Entry.FromTimer(timer));

            Task.Run(() =>
            {
                try
                {
                    string? fullPath = PathExtensions.GetAppFullPath(path, mustExist: false);

                    if (fullPath is not null)
                    {
                        ProcessExtensions.StartDetached(fullPath, json, silent);
                    }
                }
                catch (Exception ex) when (ex.CanBeHandled())
                {
                    // Ignore.
                }
            });
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            // Ignore.
        }
    }
}
