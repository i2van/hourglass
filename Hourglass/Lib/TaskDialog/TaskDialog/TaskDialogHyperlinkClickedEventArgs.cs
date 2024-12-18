﻿using System;

// ReSharper disable all

namespace KPreisser.UI;

/// <summary>
/// 
/// </summary>
public class TaskDialogHyperlinkClickedEventArgs : EventArgs
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="hyperlink"></param>
    internal TaskDialogHyperlinkClickedEventArgs(string hyperlink)
    {
        Hyperlink = hyperlink;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Hyperlink
    {
        get;
    }
}