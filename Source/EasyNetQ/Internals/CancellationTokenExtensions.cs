namespace EasyNetQ.Internals;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
public static class CancellationTokenExtensions
{
    /// <summary>
    ///     Attaches a timeout to a cancellation token
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="timeout">The timeout</param>
    /// <returns>
    ///     ValueCancellationTokenSource associated with <paramref name="cancellationToken"/>
    ///     and with <paramref name="timeout"/>
    /// </returns>
    public static ValueCancellationTokenSource WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout) => new(cancellationToken, timeout);

    /// <summary>
    /// Struct that holds a cancellation token.
    /// The idea is the same as with ValueTask vs Task - not allocate when we can.
    /// </summary>
    public readonly struct ValueCancellationTokenSource : IDisposable
    {
        private readonly CancellationTokenSource? cts = null;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Attaches a timeout to a cancellation token
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="timeout">The timeout.</param>
        public ValueCancellationTokenSource(CancellationToken cancellationToken, TimeSpan timeout)
        {
            this.cancellationToken = cancellationToken;

            if (timeout != Timeout.InfiniteTimeSpan)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, default);
                    cts.CancelAfter(timeout);
                }
                else
                {
                    cts = new CancellationTokenSource(timeout);
                }
            }
        }

        /// <summary>
        /// Gets cancellation token associated with this <see cref="ValueCancellationTokenSource"/>.
        /// </summary>
        public CancellationToken Token => cts?.Token ?? cancellationToken;

        /// <inheritdoc />
        public void Dispose() => cts?.Dispose();
    }
}
