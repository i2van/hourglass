using System.Windows.Forms;

namespace Hourglass.Windows;

/// <summary>
/// An open file dialog that is shown on a dedicated thread, so the UI stays responsive while the dialog is open.
/// </summary>
public sealed class NonBlockingOpenFileDialog : NonBlockingFileDialog
{
    /// <inheritdoc />
    protected override FileDialog CreateDialog() =>
        new OpenFileDialog
        {
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };
}
