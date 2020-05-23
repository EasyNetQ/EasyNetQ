using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.Internals
{
    public class AsyncQueueTests
    {
        [Fact]
        public async Task Should_enqueue_dequeue()
        {
            using var queue = new AsyncQueue<int>();
            queue.Enqueue(1);
            queue.Count.Should().Be(1);
            var dequeueTask = queue.DequeueAsync();
            (await dequeueTask.ConfigureAwait(false)).Should().Be(1);
            queue.Count.Should().Be(0);
        }

        [Fact]
        public async Task Should_be_able_to_cancel_dequeue()
        {
            using var queue = new AsyncQueue<int>();
            using var dequeueCts = new CancellationTokenSource();
            var dequeueTask = queue.DequeueAsync(dequeueCts.Token);
            dequeueCts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => dequeueTask).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_complete_dequeue_task()
        {
            using var queue = new AsyncQueue<int>();
            var firstDequeueTask = queue.DequeueAsync();
            var secondDequeueTask = queue.DequeueAsync();
            firstDequeueTask.IsCompleted.Should().BeFalse();
            secondDequeueTask.IsCompleted.Should().BeFalse();
            queue.Enqueue(1);
            firstDequeueTask.IsCompleted.Should().BeTrue();
            secondDequeueTask.IsCompleted.Should().BeFalse();
            (await firstDequeueTask.ConfigureAwait(false)).Should().Be(1);
        }
    }
}
