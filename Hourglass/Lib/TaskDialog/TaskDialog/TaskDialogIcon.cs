using System;
using System.Collections.Generic;
using System.Drawing;
// ReSharper disable ExceptionNotDocumented

// ReSharper disable all

namespace KPreisser.UI;

/// <summary>
/// 
/// </summary>
public abstract class TaskDialogIcon
{
    private static readonly IReadOnlyDictionary<TaskDialogStandardIcon, TaskDialogStandardIconContainer> s_standardIcons
        = new Dictionary<TaskDialogStandardIcon, TaskDialogStandardIconContainer>() {
            { TaskDialogStandardIcon.None, new(TaskDialogStandardIcon.None) },
            { TaskDialogStandardIcon.Information, new(TaskDialogStandardIcon.Information) },
            { TaskDialogStandardIcon.Warning, new(TaskDialogStandardIcon.Warning) },
            { TaskDialogStandardIcon.Error, new(TaskDialogStandardIcon.Error) },
            { TaskDialogStandardIcon.SecurityShield, new(TaskDialogStandardIcon.SecurityShield) },
            { TaskDialogStandardIcon.SecurityShieldBlueBar, new(TaskDialogStandardIcon.SecurityShieldBlueBar) },
            { TaskDialogStandardIcon.SecurityShieldGrayBar, new(TaskDialogStandardIcon.SecurityShieldGrayBar) },
            { TaskDialogStandardIcon.SecurityWarningYellowBar, new(TaskDialogStandardIcon.SecurityWarningYellowBar) },
            { TaskDialogStandardIcon.SecurityErrorRedBar, new(TaskDialogStandardIcon.SecurityErrorRedBar) },
            { TaskDialogStandardIcon.SecuritySuccessGreenBar, new(TaskDialogStandardIcon.SecuritySuccessGreenBar) },
        };

    private protected TaskDialogIcon()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="icon"></param>
    public static implicit operator TaskDialogIcon(TaskDialogStandardIcon icon) =>
        s_standardIcons.TryGetValue(icon, out TaskDialogStandardIconContainer result)
            ? result
            : throw new InvalidCastException();

#if !NET_STANDARD
    /// <summary>
    /// 
    /// </summary>
    /// <param name="icon"></param>
    public static implicit operator TaskDialogIcon(Icon icon)
    {
        return new TaskDialogIconHandle(icon);
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    /// <param name="icon"></param>
    /// <returns></returns>
    public static TaskDialogIcon Get(TaskDialogStandardIcon icon)
    {
        if (!s_standardIcons.TryGetValue(icon, out TaskDialogStandardIconContainer result))
            throw new ArgumentOutOfRangeException(nameof(icon));

        return result;
    }
}