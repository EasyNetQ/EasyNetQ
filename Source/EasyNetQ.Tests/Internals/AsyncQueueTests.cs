using System;
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
        public async Task Should_be_empty_after_dequeue()
        {
            using var queue = new AsyncQueue<int>(new[] {42});
            var element = await queue.DequeueAsync(CancellationToken.None);
            element.Should().Be(42);
            queue.Count.Should().Be(0);
        }

        [Fact]
        public void Should_be_not_empty_after_enqueue()
        {
            using var queue = new AsyncQueue<int>();
            queue.Enqueue(42);
            queue.Count.Should().Be(1);
        }

        [Fact]
        public async Task Should_be_able_to_cancel_dequeue()
        {
            using var queue = new AsyncQueue<int>();
            using var dequeueCts = new CancellationTokenSource();
            var dequeueTask = queue.DequeueAsync(dequeueCts.Token);
            var _ = Task.Run(dequeueCts.Cancel, CancellationToken.None);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeueTask);
        }

        [Fact]
        public async Task Should_complete_dequeue_task_in_order()
        {
            using var queue = new AsyncQueue<int>();
            var firstTask = queue.DequeueAsync();
            var secondTask = queue.DequeueAsync();
            var thirdTask = queue.DequeueAsync();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);
            (await firstTask).Should().Be(1);
            (await secondTask).Should().Be(2);
            (await thirdTask).Should().Be(3);
            queue.Count.Should().Be(0);
        }
    }
}
