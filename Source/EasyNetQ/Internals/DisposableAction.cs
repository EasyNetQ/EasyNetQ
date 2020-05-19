using System;

namespace EasyNetQ.Internals
{
    internal struct DisposableAction<TState> : IDisposable
    {
        public static DisposableAction<TState> Create(Action<TState> action, TState state)
        {
            return new DisposableAction<TState>(action, state);
        }

        private readonly Action<TState> action;
        private readonly TState state;

        private DisposableAction(Action<TState> action, TState state)
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
