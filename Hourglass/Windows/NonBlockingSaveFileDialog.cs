using System.Windows.Forms;

namespace Hourglass.Windows;

/// <summary>
/// A save file dialog that is shown on a dedicated thread, so the UI stays responsive while the dialog is open.
/// </summary>
public sealed class NonBlockingSaveFileDialog : NonBlockingFileDialog
{
    /// <summary>
    /// Gets the default file name extension, which is added if the user does not specify an extension.
    /// </summary>
    public string? DefaultExt { get; init; }

    /// <inheritdoc />
    protected override FileDialog CreateDialog() =>
        new SaveFileDialog
        {
            DefaultExt = DefaultExt,
            AddExtension = true,
            OverwritePrompt = true
        };
}
