using System;

namespace Hourglass.Extensions;

internal static class DisposableExtensions
{
    public static IDisposable CreateDisposable(this Action action) =>
        new DisposableAction(action);

    private class DisposableAction : IDisposable
    {
        private readonly Action _action;

        public DisposableAction(Action action) =>
            _action = action;

        public void Dispose() =>
            _action();
    }
}