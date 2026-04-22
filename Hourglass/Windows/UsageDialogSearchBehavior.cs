namespace Hourglass.Windows;

using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
/// Encapsulates the search bar logic for <see cref="UsageDialog"/>.
/// </summary>
internal sealed class UsageDialogSearchBehavior
{
    private static readonly RoutedCommand OpenSearchCommand = new();
    private static readonly RoutedCommand SearchNextCommand = new();
    private static readonly RoutedCommand SearchPrevCommand = new();

    private readonly TextBox _usageTextBox;
    private readonly Border _searchBar;
    private readonly TextBox _searchTextBox;
    private readonly TextBlock _matchCountTextBlock;

    private readonly BitArray _searchMatches;

    private int _searchMatchCount;
    private int _currentMatchCharPos = -1;

    private bool _programmaticFocus;

    public UsageDialogSearchBehavior(
        Window window,
        TextBox usageTextBox,
        Border searchBar,
        TextBox searchTextBox,
        TextBlock matchCountTextBlock)
    {
        _usageTextBox = usageTextBox;
        _searchBar = searchBar;
        _searchTextBox = searchTextBox;
        _matchCountTextBlock = matchCountTextBlock;

        _searchMatches = new(usageTextBox.Text.Length);

        // '/' is handled via PreviewTextInput for keyboard-layout independence.
        // Escape hides the search bar (if visible) via PreviewKeyDown; when the bar is
        // hidden the IsCancel Close button takes over and closes the window.
        window.PreviewTextInput += OnWindowPreviewTextInput;
        window.PreviewKeyDown   += OnWindowPreviewKeyDown;

        window.InputBindings.Add(new KeyBinding(SearchNextCommand, Key.F3, ModifierKeys.None));
        window.InputBindings.Add(new KeyBinding(SearchPrevCommand, Key.F3, ModifierKeys.Shift));
        window.InputBindings.Add(new KeyBinding(OpenSearchCommand, Key.F,  ModifierKeys.Control));

        // Enter bindings are scoped to the search box so they don't steal Enter
        // from the rest of the dialog (e.g. the Close button's IsDefault behaviour).
        _searchTextBox.InputBindings.Add(new KeyBinding(SearchNextCommand, Key.Enter, ModifierKeys.None));
        _searchTextBox.InputBindings.Add(new KeyBinding(SearchPrevCommand, Key.Enter, ModifierKeys.Shift));

        window.CommandBindings.Add(new(OpenSearchCommand, OnOpenSearch));
        window.CommandBindings.Add(new(SearchNextCommand, OnSearchNext));
        window.CommandBindings.Add(new(SearchPrevCommand, OnSearchPrev));

        _usageTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnUsageTextBoxScrollChanged));
        _usageTextBox.GotKeyboardFocus += OnUsageTextBoxGotKeyboardFocus;
        _usageTextBox.PreviewMouseDown += OnUsageTextBoxPreviewMouseDown;

        _searchTextBox.LostKeyboardFocus += OnSearchTextBoxLostKeyboardFocus;
        _searchTextBox.TextChanged += OnSearchTextBoxTextChanged;
    }

    private void OnOpenSearch(object sender, ExecutedRoutedEventArgs e)
    {
        if (_searchBar.Visibility != Visibility.Visible)
        {
            ShowSearchBar();
        }
    }

    private void OnSearchNext(object sender, ExecutedRoutedEventArgs e)
    {
        if (_searchBar.Visibility != Visibility.Visible)
        {
            ShowSearchBar();
        }
        else
        {
            NavigateNext();
        }
    }

    private void OnSearchPrev(object sender, ExecutedRoutedEventArgs e)
    {
        if (_searchBar.Visibility == Visibility.Visible)
        {
            NavigatePrev();
        }
    }

    private void OnWindowPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == "/" && _searchBar.Visibility != Visibility.Visible)
        {
            ShowSearchBar();
            e.Handled = true;
        }
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _searchBar.Visibility == Visibility.Visible)
        {
            HideSearchBar();
            e.Handled = true;
        }
    }

    private void OnUsageTextBoxScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_currentMatchCharPos >= 0 && _searchMatchCount > 0 && Mouse.LeftButton != MouseButtonState.Pressed)
        {
            _usageTextBox.Select(_currentMatchCharPos, _searchTextBox.Text.Length);
        }
    }

    private void OnUsageTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_programmaticFocus && _searchBar.Visibility == Visibility.Visible)
        {
            HideSearchBar();
        }
    }

    private void OnUsageTextBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_searchBar.Visibility == Visibility.Visible)
        {
            HideSearchBar();
        }
    }

    private void OnSearchTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_programmaticFocus && _searchBar.Visibility == Visibility.Visible)
        {
            HideSearchBar();
        }
    }

    private void OnSearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSearch();
    }

    private void ShowSearchBar()
    {
        _searchBar.Visibility = Visibility.Visible;
        _searchTextBox.Focus();
        _searchTextBox.SelectAll();
    }

    private void HideSearchBar()
    {
        _searchBar.Visibility = Visibility.Collapsed;
        ClearUsageSelection();
        _matchCountTextBlock.Text = string.Empty;
        _matchCountTextBlock.Visibility = Visibility.Collapsed;
        _searchTextBox.Text = string.Empty;
        _searchMatches.SetAll(false);
        _searchMatchCount = 0;
        _currentMatchCharPos = -1;
    }

    private void ClearUsageSelection()
    {
        _programmaticFocus = true;
        _usageTextBox.Focus();
        _usageTextBox.Select(_usageTextBox.SelectionStart, 0);
        _programmaticFocus = false;
        if (_searchBar.Visibility == Visibility.Visible)
        {
            _searchTextBox.Focus();
        }
    }

    private void UpdateSearch()
    {
        var searchText = _searchTextBox.Text;
        var text = _usageTextBox.Text;

        _searchMatches.SetAll(false);
        _searchMatchCount = 0;
        _currentMatchCharPos = -1;

        if (string.IsNullOrEmpty(searchText))
        {
            ClearUsageSelection();
            _matchCountTextBlock.Text = string.Empty;
            _matchCountTextBlock.Visibility = Visibility.Collapsed;
            return;
        }

        var index = 0;

        while (true)
        {
            index = text.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                break;
            }

            _searchMatches[index] = true;
            _searchMatchCount++;
            index += searchText.Length;
        }

        if (_searchMatchCount > 0)
        {
            NavigateToMatch(FindNextMatchPos(-1));
        }
        else
        {
            ClearUsageSelection();
            _matchCountTextBlock.Text = Properties.Resources.UsageDialogSearchNoMatchText;
            _matchCountTextBlock.Foreground = Brushes.Red;
            _matchCountTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void NavigateToMatch(int charPos)
    {
        _currentMatchCharPos = charPos;
        _programmaticFocus = true;
        _usageTextBox.Focus();
        _programmaticFocus = false;
        _usageTextBox.Select(charPos, _searchTextBox.Text.Length);
        _usageTextBox.ScrollToLine(_usageTextBox.GetLineIndexFromCharacterIndex(charPos));
        _searchTextBox.Focus();
        _matchCountTextBlock.Text = $"{GetMatchOrdinal(charPos)}/{_searchMatchCount}";
        _matchCountTextBlock.Foreground = SystemColors.ControlTextBrush;
        _matchCountTextBlock.Visibility = Visibility.Visible;
    }

    private int FindNextMatchPos(int fromCharPos)
    {
        for (var i = fromCharPos + 1; i < _searchMatches.Length; i++)
        {
            if (_searchMatches[i])
            {
                return i;
            }
        }

        for (var i = 0; i <= fromCharPos; i++)
        {
            if (_searchMatches[i])
            {
                return i;
            }
        }

        return -1;
    }

    private int FindPrevMatchPos(int fromCharPos)
    {
        for (var i = fromCharPos - 1; i >= 0; i--)
        {
            if (_searchMatches[i])
            {
                return i;
            }
        }

        for (var i = _searchMatches.Length - 1; i >= fromCharPos; i--)
        {
            if (_searchMatches[i])
            {
                return i;
            }
        }

        return -1;
    }

    private int GetMatchOrdinal(int charPos)
    {
        var ordinal = 0;

        for (var i = 0; i <= charPos && i < _searchMatches.Length; i++)
        {
            if (_searchMatches[i])
            {
                ordinal++;
            }
        }

        return ordinal;
    }

    private void NavigateNext()
    {
        if (_searchMatchCount == 0)
        {
            return;
        }

        NavigateToMatch(FindNextMatchPos(_currentMatchCharPos));
    }

    private void NavigatePrev()
    {
        if (_searchMatchCount == 0)
        {
            return;
        }

        NavigateToMatch(FindPrevMatchPos(_currentMatchCharPos));
    }
}
