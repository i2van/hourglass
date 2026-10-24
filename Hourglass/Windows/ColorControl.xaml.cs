// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorControl.xaml.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

using Hourglass.Extensions;
using Hourglass.Timing;

namespace Hourglass.Windows;

// ReSharper disable MismatchedFileName

/// <summary>
/// A control for displaying and selecting a <see cref="Color"/>.
/// </summary>
public sealed partial class ColorControl
{
    /// <summary>
    /// A <see cref="DependencyProperty"/> that specifies the text label.
    /// </summary>
    public static readonly DependencyProperty TextProperty
        = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ColorControl));

    /// <summary>
    /// A <see cref="DependencyProperty"/> that specifies the color.
    /// </summary>
    public static readonly DependencyProperty ColorProperty
        = DependencyProperty.Register(nameof(Color), typeof(Color), typeof(ColorControl));

    /// <summary>
    /// A <see cref="DependencyProperty"/> that specifies the theme to which the color belongs.
    /// </summary>
    public static readonly DependencyProperty ThemeProperty
        = DependencyProperty.Register(nameof(Theme), typeof(Theme), typeof(ColorControl));

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorControl"/> class.
    /// </summary>
    public ColorControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Occurs when the <see cref="Color"/> property changes.
    /// </summary>
    public event EventHandler? ColorChanged;

    /// <summary>
    /// Gets or sets the text label.
    /// </summary>
    public string Text
    {
        get => (string) GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public Color Color
    {
        get => (Color) GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the theme to which the color belongs.
    /// </summary>
    public Theme Theme
    {
        get => (Theme) GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    /// <summary>
    /// Invoked when the <see cref="Button"/> is clicked.
    /// </summary>
    /// <param name="sender">The <see cref="Button"/>.</param>
    /// <param name="e">The event data.</param>
    private void ButtonClick(object sender, RoutedEventArgs e)
    {
        ColorDialog dialog = new()
        {
            AnyColor = true,
            FullOpen = true,
            CustomColors = Theme.GetPalette().Select(static c => c.ToInt()).ToArray()
        };

        DialogResult result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
            Color = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
            ColorChanged?.Invoke(this /* sender */, EventArgs.Empty);
        }
    }
}