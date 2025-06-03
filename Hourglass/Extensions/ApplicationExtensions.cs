using System.Windows;
using System.Windows.Shell;

namespace Hourglass.Extensions;

using Properties;

internal static class ApplicationExtensions
{
    public static void ClearJumpList(this Application? application)
    {
        if (!Settings.Default.UseJumpList)
        {
            return;
        }

        try
        {
            if (application is null)
            {
                return;
            }

            var jumpList = JumpList.GetJumpList(application);

            jumpList?.JumpItems.Clear();
            jumpList?.Apply();
        }
        catch
        {
            // Ignore.
        }
    }
}

