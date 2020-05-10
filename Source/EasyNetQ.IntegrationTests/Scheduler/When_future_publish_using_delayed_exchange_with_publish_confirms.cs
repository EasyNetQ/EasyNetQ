﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using EasyNetQ.Scheduling;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Scheduler
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_delay_using_delay_exchange_with_publish_confirms : IDisposable
    {
        public When_publish_and_subscribe_with_delay_using_delay_exchange_with_publish_confirms(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus(
                $"host={fixture.Host};prefetchCount=1;publisherConfirms=True",
                c => c.EnableDelayedExchangeScheduler()
            );
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private const int MessagesCount = 10;

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var subscriptionId = Guid.NewGuid().ToString();
            var messagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);

            using (bus.Subscribe<Message>(subscriptionId, messagesSink.Receive))
            {
                await bus.FuturePublishBatchAsync(messages, TimeSpan.FromSeconds(5), timeoutCts.Token)
                    .ConfigureAwait(false);

                await messagesSink.WaitAllReceivedAsync(timeoutCts.Token).ConfigureAwait(false);
                messagesSink.ReceivedMessages.Should().Equal(messages);
            }
        }
    }
}
