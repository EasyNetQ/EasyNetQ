using System;
using System.Threading;

namespace EasyNetQ.Internals
{
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
        ///     CancellationTokenSource associated with <paramref name="cancellationToken"/>
        ///     and with <paramref name="timeout"/>
        /// </returns>
        public static CancellationTokenSource WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, default);
            if (timeout != Timeout.InfiniteTimeSpan)
                cts.CancelAfter(timeout);
            return cts;
        }
    }
}
