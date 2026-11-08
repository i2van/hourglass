using System;

using Hourglass.Properties;

namespace Hourglass;

internal static class Urls
{
    public static readonly Uri FAQ      = new(Resources.FAQUrl);
    public static readonly Uri Usage    = new(Resources.UsageUrl);
    public static readonly Uri Readme   = new(Resources.ReadmeUrl);
    public static readonly Uri NewIssue = new(Resources.NewIssueUrl);

    public static readonly Uri ErrorLogsHelp     = new(FAQ, "#how-to-find-the-hourglass-error-logs");
    public static readonly Uri ThemesHelp        = new(FAQ, "#how-do-i-add-a-custom-color-theme");
    public static readonly Uri PromptOnCloseHelp = new(FAQ, "#how-do-i-stop-the-timer-from-prompting-me-when-i-close-the-window");
}
