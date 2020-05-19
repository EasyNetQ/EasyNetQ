using System;

namespace EasyNetQ.Internals
{
    internal static class DisposableActions
    {
        public static DisposableAction<TState> Create<TState>(Action<TState> action, TState state)
        {
            return new DisposableAction<TState>(action, state);
        }
    }

    internal readonly struct DisposableAction<TState> : IDisposable
    {
        private readonly Action<TState> action;
        private readonly TState state;

        public DisposableAction(Action<TState> action, TState state)
        {
            this.action = action;
            this.state = state;
        }

        public void Dispose()
        {
            action(state);
        }
    }
}
