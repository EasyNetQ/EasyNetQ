using System;

namespace EasyNetQ.Internals
{
    internal struct DisposableCallback<TState> : IDisposable
    {
        public static DisposableCallback<TState> Create(Action<TState> action, TState state)
        {
            return new DisposableCallback<TState>(action, state);
        }

        private readonly Action<TState> action;
        private readonly TState state;

        public DisposableCallback(Action<TState> action, TState state)
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
