// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorDialog.xaml.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

using Extensions;

using static System.Windows.SystemParameters;

// ReSharper disable MismatchedFileName

/// <summary>
/// A window that displays an error.
/// </summary>
public sealed partial class ErrorDialog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDialog"/> class.
    /// </summary>
    public ErrorDialog()
    {
        InitializeComponent();
        InitializeResources();
        InitializeMaxSize();
    }

    /// <summary>
    /// Opens the window and returns only when the window is closed.
    /// </summary>
    /// <param name="title">The title for the error dialog.</param>
    /// <param name="message">The error message to show. (Optional.)</param>
    /// <param name="details">Details of the error, such as a call stack. (Optional.)</param>
    public void ShowDialog(string title, string? message = null, string? details = null)
    {
        Application.Current.ClearJumpList();

        TitleTextBlock.Text = title;

        MessageTextBox.Text = message ?? string.Empty;
        MessageBorder.Visibility = string.IsNullOrWhiteSpace(message).ToVisibilityReversed();

        DetailsTextBox.Text = details ?? string.Empty;
        ShowDetailsButton.IsEnabled = !string.IsNullOrWhiteSpace(details);

        ShowDialog();
    }

    /// <summary>
    /// Initializes localized resources.
    /// </summary>
    private void InitializeResources()
    {
        Title = Properties.Resources.ErrorDialogTitle;
        TitleTextBlock.Text = Properties.Resources.ErrorDialogDefaultMessageText;
        ShowDetailsButton.Content = Properties.Resources.ErrorDialogShowDetailsButtonContent;
        CloseButton.Content = Properties.Resources.ErrorDialogCloseButtonContent;
    }

    /// <summary>
    /// Initializes the <see cref="Window.MaxWidth"/> and <see cref="Window.MaxHeight"/> properties.
    /// </summary>
    private void InitializeMaxSize()
    {
        MaxWidth = 0.75 * WorkArea.Width;

        double maxHeight = 0.75 * WorkArea.Height;

        TitleTextBlock.MaxHeight = maxHeight * 0.1;
        MessageTextBox.MaxHeight = maxHeight * 0.15;
        DetailsTextBox.MaxHeight = maxHeight * 0.75;
    }

    /// <summary>
    /// Invoked when the <see cref="ShowDetailsButton"/> is clicked.
    /// </summary>
    /// <param name="sender">The <see cref="ShowDetailsButton"/>.</param>
    /// <param name="e">The event data.</param>
    private void ShowDetailsButtonClick(object sender, RoutedEventArgs e)
    {
        if (DetailsBorder.Visibility != Visibility.Visible)
        {
            DetailsBorder.Visibility = Visibility.Visible;
            ShowDetailsButton.IsEnabled = false;
            CloseButton.Focus();

            Dispatcher.InvokeAsync(delegate
            {
                Left = WorkArea.Left + (WorkArea.Width - ActualWidth) / 2;
                Top = WorkArea.Top + (WorkArea.Height - ActualHeight) / 2;
            }, DispatcherPriority.Loaded);
        }
    }

    /// <summary>
    /// Invoked when the copy to clipboard link is clicked.
    /// </summary>
    /// <param name="sender">The hyperlink.</param>
    /// <param name="e">The event data.</param>
    private void CopyToClipboardButtonClick(object sender, RoutedEventArgs e)
    {
        string text = string.Join(Environment.NewLine + Environment.NewLine,
            new[] { TitleTextBlock.Text, MessageTextBox.Text, DetailsTextBox.Text }
                .Where(static s => !string.IsNullOrWhiteSpace(s)));

        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // Ignore.
        }
    }

    /// <summary>
    /// Invoked when the report bug link is clicked.
    /// </summary>
    /// <param name="sender">The hyperlink.</param>
    /// <param name="e">The event data.</param>
    private void ReportBugButtonClick(object sender, RoutedEventArgs e)
    {
        string title =
#if PORTABLE
            "[Portable] " +
#endif
            TitleTextBlock.Text;

        string[] parts = [..
            new[] { MessageTextBox.Text, DetailsTextBox.Text }
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(RemoveFullPaths)
        ];

        string body = parts.Length > 0
            ? $"```text{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, parts)}{Environment.NewLine}```"
            : string.Empty;

        string url = $"{Urls.NewIssue}?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";

        try
        {
            new Uri(url).Navigate();
        }
        catch
        {
            // Ignore.
        }

        static string RemoveFullPaths(string text) =>
            Regex.Replace(text, """(?<=["'])(?:[A-Za-z]:\\|\\\\|/)(?:[^"'/\\\r\n]+[/\\])+""", @"***\");
    }
}