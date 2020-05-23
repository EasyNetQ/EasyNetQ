using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using Xunit;

namespace EasyNetQ.Tests.Internals
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task Should_cancel_when_cancellation_attached()
        {
             var cts = new CancellationTokenSource();
             var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
             tcs.AttachCancellation(cts.Token);
             cts.Cancel();
             await Assert.ThrowsAsync<TaskCanceledException>(() => tcs.Task).ConfigureAwait(false);
        }

        [Fact]
        public void Should_throw_if_tcs_has_no_supported_flag()
        {
            var tcs = new TaskCompletionSource<object>();
            Assert.Throws<ArgumentOutOfRangeException>(
                () => tcs.AttachCancellation(CancellationToken.None)
            );
        }
    }
}
