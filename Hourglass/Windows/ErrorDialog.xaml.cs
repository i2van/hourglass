// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorDialog.xaml.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows;

using System.Windows;

using Extensions;

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
    /// <param name="title">The title for the error dialog..</param>
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
        MaxWidth = 0.75 * SystemParameters.WorkArea.Width;

        double maxHeight = 0.75 * SystemParameters.WorkArea.Height;

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
        }
    }
}