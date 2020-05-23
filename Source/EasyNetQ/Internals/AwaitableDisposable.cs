using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public readonly struct AwaitableDisposable<T> where T : IDisposable
    {
        /// <summary>
        ///     The underlying task.
        /// </summary>
        private readonly Task<T> task;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AwaitableDisposable{T}" /> struct -
        ///     awaitable wrapper around the specified task.
        /// </summary>
        /// <param name="task">The underlying task to wrap. This may not be <c>null</c>.</param>
        public AwaitableDisposable(Task<T> task)
        {
            this.task = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        ///     Returns the underlying task.
        /// </summary>
        public Task<T> AsTask()
        {
            return task;
        }

        /// <summary>
        ///     Implicit conversion to the underlying task.
        /// </summary>
        /// <param name="source">The awaitable wrapper.</param>
        public static implicit operator Task<T>(AwaitableDisposable<T> source)
        {
            return source.AsTask();
        }

        /// <summary>
        ///     Returns the task awaiter for the underlying task.
        /// </summary>
        public TaskAwaiter<T> GetAwaiter()
        {
            return task.GetAwaiter();
        }

        /// <summary>
        ///     Returns a configured task awaiter for the underlying task.
        /// </summary>
        /// <param name="continueOnCapturedContext">Whether to attempt to marshal the continuation back to the captured context.</param>
        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
        {
            return task.ConfigureAwait(continueOnCapturedContext);
        }
    }
}
