﻿using System;

// ReSharper disable all

namespace KPreisser.UI;

/// <summary>
/// 
/// </summary>
public class TaskDialogButtonClickedEventArgs : EventArgs
{
    /// <summary>
    /// 
    /// </summary>
    internal TaskDialogButtonClickedEventArgs()
    {
    }

    /// <summary>
    /// Gets or sets a value that indicates if the dialog should not be closed
    /// after the event handler returns.
    /// </summary>
    /// <remarks>
    /// When you don't set this property to <c>true</c>, the
    /// <see cref="TaskDialog.Closing"/> event will occur afterwards.
    /// </remarks>
    public bool CancelClose
    {
        get;
        set;
    }
}