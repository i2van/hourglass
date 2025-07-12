// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WatermarkAdorner.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

using Extensions;

/// <summary>
/// An <see cref="Adorner"/> that displays a watermark on a <see cref="TextBox"/> or <see cref="ComboBox"/> control.
/// </summary>
/// <seealso cref="Hint"/>
public sealed class WatermarkAdorner : Adorner
{
    /// <summary>
    /// A <see cref="ContentPresenter"/> that contains the watermark.
    /// </summary>
    private readonly ContentPresenter _contentPresenter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatermarkAdorner"/> class.
    /// </summary>
    /// <param name="adornedElement">The <see cref="UIElement"/> to apply the watermark to.</param>
    /// <param name="hint">The content of the watermark, typically a <see cref="string"/>.</param>
    /// <param name="brush">The foreground of the watermark.</param>
    public WatermarkAdorner(UIElement adornedElement, object hint, Brush brush)
        : base(adornedElement)
    {
        _contentPresenter = new()
        {
            Content = hint,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        TextBlock.SetForeground(_contentPresenter, brush);

        IsHitTestVisible = false;

        AdornedElement.IsVisibleChanged += AdornedElementIsVisibleChanged;
        Visibility = AdornedElement.IsVisible.ToVisibility();

        Binding opacityBinding = new()
        {
            Source = AdornedElement,
            Path = new(nameof(Opacity))
        };
        BindingOperations.SetBinding(this, OpacityProperty, opacityBinding);
    }

    /// <summary>
    /// Gets or sets the content of the watermark.
    /// </summary>
    public object Hint
    {
        get => _contentPresenter.Content;
        set => _contentPresenter.Content = value;
    }

    /// <summary>
    /// Gets or sets the foreground of the watermark.
    /// </summary>
    public Brush Brush
    {
        get => TextBlock.GetForeground(_contentPresenter);
        set => TextBlock.SetForeground(_contentPresenter, value);
    }

    /// <inheritdoc />
    protected override int VisualChildrenCount => 1;

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _contentPresenter.Arrange(new(finalSize));

        switch (AdornedElement)
        {
            case TextBox textBox:
                TextElement.SetFontFamily(_contentPresenter, textBox.FontFamily);
                TextElement.SetFontSize(_contentPresenter, textBox.FontSize);
                break;
            case ComboBox comboBox:
                TextElement.SetFontFamily(_contentPresenter, comboBox.FontFamily);
                TextElement.SetFontSize(_contentPresenter, comboBox.FontSize);
                break;
        }

        return finalSize;
    }

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index)
    {
        return index != 0
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : _contentPresenter;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        _contentPresenter.Measure(AdornedElement.RenderSize);
        return AdornedElement.RenderSize;
    }

    /// <summary>
    /// Invoked when the value of the <see cref="UIElement.IsVisible"/> property changes on the <see
    /// cref="Adorner.AdornedElement"/>.
    /// </summary>
    /// <param name="sender">The <see cref="Adorner.AdornedElement"/>.</param>
    /// <param name="e">The event data.</param>
    private void AdornedElementIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Visibility = AdornedElement.IsVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}