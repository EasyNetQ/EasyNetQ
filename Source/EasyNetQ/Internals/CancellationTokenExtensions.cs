using System;
using System.Threading;

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
    public static ValueCancellationTokenSource WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        return new ValueCancellationTokenSource(cancellationToken, timeout);
    }

    /// <summary>
    /// Struct that holds a cancellation token.
    /// The idea is the same as with ValueTask vs Task - not allocate when we can.
    /// </summary>
    public readonly struct ValueCancellationTokenSource : IDisposable
    {
        private readonly CancellationTokenSource _cts = null;
        private readonly CancellationToken _token;

        /// <summary>
        /// Attaches a timeout to a cancellation token
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="timeout">The timeout.</param>
        public ValueCancellationTokenSource(CancellationToken cancellationToken, TimeSpan timeout)
        {
            _token = cancellationToken;

            if (timeout != Timeout.InfiniteTimeSpan)
            {
                if (cancellationToken == default)
                {
                    _cts = new CancellationTokenSource(timeout);
                }
                else
                {
                    // use 'default' as second argument just because only that overload except 'params' one is available for netstandard2.0
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, default);
                    _cts.CancelAfter(timeout);
                }
            }
        }

        /// <summary>
        /// Gets cancellation token associated with this <see cref="ValueCancellationTokenSource"/>.
        /// </summary>
        public CancellationToken Token => _cts == null ? _token : _cts.Token;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_cts != null)
                _cts.Dispose();
        }
    }
}
