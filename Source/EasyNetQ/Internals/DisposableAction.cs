using System;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static class DisposableAction
    {
        /// <summary>
        /// Wraps an action and a state with <see cref="DisposableAction{TState}"/>
        /// </summary>
        /// <param name="action">The action</param>
        /// <param name="state">The state</param>
        /// <typeparam name="TState">The type of state</typeparam>
        /// <returns>Returns <see cref="DisposableAction{TState}"/></returns>
        public static DisposableAction<TState> Create<TState>(Action<TState> action, TState state)
        {
            return new DisposableAction<TState>(action, state);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public readonly struct DisposableAction<TState> : IDisposable
    {
        private readonly Action<TState> action;
        private readonly TState state;

        /// <summary>
        /// Creates a disposable action from an action with a state
        /// </summary>
        /// <param name="action">The action</param>
        /// <param name="state">The state</param>
        public DisposableAction(Action<TState> action, TState state)
        {
            this.action = action;
            this.state = state;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            action(state);
        }
    }
}
