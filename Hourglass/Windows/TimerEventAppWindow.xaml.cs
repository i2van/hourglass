using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Hourglass.Extensions;
using Hourglass.Managers;
using Hourglass.Properties;

namespace Hourglass.Windows;

// ReSharper disable MismatchedFileName

/// <summary>
/// A window that selects the app to run on the timer events.
/// </summary>
public sealed partial class TimerEventAppWindow
{
    /// <summary>
    /// The instance of the <see cref="TimerEventAppWindow"/> that is showing, or null if there is no instance showing.
    /// </summary>
    private static TimerEventAppWindow? _instance;

    /// <summary>
    /// The app selection dialog that is shown, or <see langword="null"/> if the dialog is not shown.
    /// </summary>
    private NonBlockingOpenFileDialog? _browseDialog;

    /// <summary>
    /// A value indicating whether the window is closed.
    /// </summary>
    private bool _closed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerEventAppWindow"/> class.
    /// </summary>
    public TimerEventAppWindow()
    {
        InitializeComponent();

        Settings settings = Settings.Default;

        PathTextBox.Text = settings.TimerEventAppPath;
        DisabledCheckBox.IsChecked = settings.TimerEventAppDisabled;
        SilentCheckBox.IsChecked = settings.TimerEventAppSilent;
    }

    /// <summary>
    /// Shows or activates the <see cref="TimerEventAppWindow"/>. Call this method instead of the constructor to
    /// prevent multiple instances of the window.
    /// </summary>
    public static void ShowOrActivate()
    {
        if (_instance is null)
        {
            _instance = new();
            _instance.Show();
        }
        else
        {
            _instance.Activate();
        }
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        this.ShowHelpButton(Urls.TimerEventAppHelp);
        this.RemoveDisabledSystemMenuItems();
        DialogZOrderManager.Instance.Register(this);

        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WindowProc);

        base.OnSourceInitialized(e);
    }

    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // The window height is fixed, so the window is maximized horizontally only.
        if (msg == NativeMethods.WM_GETMINMAXINFO)
        {
            WindowExtensions.SetMaximizedBoundsToWorkArea(hwnd, lParam, horizontalOnly: true);
        }

        return IntPtr.Zero;
    }

    /// <inheritdoc />
    protected override void OnActivated(EventArgs e)
    {
        Application.Current.ClearJumpList();

        base.OnActivated(e);
    }

    private void ShowMessage(string? errorMessage = null)
    {
        if (errorMessage is not null)
        {
            MessagePanel.Background = new SolidColorBrush(Color.FromRgb(199, 80, 80));
            MessageTextBlock.Text = errorMessage;
        }
        else
        {
            MessagePanel.Background = Brushes.Gray;
            MessageTextBlock.Text = Properties.Resources.TimerEventAppWindowDefaultMessageText;
        }
    }

    private void PathTextBoxTextChanged(object sender, TextChangedEventArgs e) =>
        ShowMessage();

    /// <summary>
    /// Returns the one-based browse dialog filter index (programs, scripts or all files) matching the <paramref
    /// name="path"/> extension.
    /// </summary>
    /// <param name="path">The application path.</param>
    /// <returns>The one-based browse dialog filter index.</returns>
    private static int GetBrowseDialogFilterIndex(string? path) =>
        Path.GetExtension(path)?.ToLowerInvariant() switch
        {
            null or "" or ".exe" or ".com" => 1,
            ".bat" or ".cmd" => 2,
            _ => 3
        };

    private async void BrowseButtonClick(object sender, RoutedEventArgs e)
    {
        if (_browseDialog is not null)
        {
            _browseDialog.Activate();
            return;
        }

        string? fullPath = PathExtensions.GetAppFullPath(PathTextBox.Text);

        NonBlockingOpenFileDialog browseDialog = new()
        {
            Title = Properties.Resources.TimerEventAppWindowBrowseDialogTitle,
            Filter = Properties.Resources.TimerEventAppWindowBrowseDialogFilter,
            FilterIndex = GetBrowseDialogFilterIndex(fullPath),
            FileName = fullPath is null ? null : Path.GetFileName(fullPath),
            InitialDirectory = fullPath is null
                ? AssemblyExtensions.GetExecutableDirectoryName()
                : Path.GetDirectoryName(fullPath)
        };

        _browseDialog = browseDialog;

        try
        {
            string? path = await browseDialog.ShowAsync(this);

            if (path is not null && !_closed)
            {
                PathTextBox.Text = PathExtensions.ToAppRelativePath(path);
            }
        }
        catch (Exception ex) when (ex.CanBeHandled())
        {
            // Ignore.
        }
        finally
        {
            _browseDialog = null;
        }
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        // The initial size fits the content, so it is the minimum size. The content is then stretched with the window
        // horizontally, while the height stays fixed.
        MinWidth = ActualWidth;
        MinHeight = ActualHeight;
        MaxHeight = ActualHeight;
        Width = ActualWidth;
        Height = ActualHeight;
        SizeToContent = SizeToContent.Manual;
        RootPanel.Width = double.NaN;

        // The startup location is calculated before the window is sized to the content, so the window is centered here.
        this.CenterOnScreen();
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        string path = PathTextBox.Text;

        if (!string.IsNullOrWhiteSpace(path) && PathExtensions.GetAppFullPath(path) is null)
        {
            ShowMessage(Properties.Resources.TimerEventAppWindowAppNotFoundText);
            PathTextBox.Focus();
            PathTextBox.SelectAll();
            return;
        }

        Settings settings = Settings.Default;

        settings.TimerEventAppPath = PathExtensions.ToAppRelativePath(path);
        settings.TimerEventAppDisabled = DisabledCheckBox.IsChecked == true;
        settings.TimerEventAppSilent = SilentCheckBox.IsChecked == true;

        AppManager.Instance.Persist();

        Close();
    }

    private void CancelButtonClick(object sender, RoutedEventArgs e) =>
        Close();

    private void WindowClosed(object sender, EventArgs e)
    {
        _closed = true;

        _browseDialog?.Close();

#pragma warning disable S2696
        _instance = null;
#pragma warning restore S2696
    }
}
