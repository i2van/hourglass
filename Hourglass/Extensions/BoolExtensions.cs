using System.Windows;

namespace Hourglass.Extensions;

internal static class BoolExtensions
{
    extension(bool visible)
    {
        public Visibility ToVisibility() =>
            visible ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ToVisibilityReversed() =>
            ToVisibility(!visible);
    }
}
