using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <inheritdoc />
    public class PublishConfirmationListener : IPublishConfirmationListener
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<ulong, TaskCompletionSource<object>>> unconfirmedChannelRequests = new ConcurrentDictionary<int, ConcurrentDictionary<ulong, TaskCompletionSource<object>>>();
        private readonly IDisposable[] subscriptions;

        /// <summary>
        /// Creates publish confirmations listener
        /// </summary>
        /// <param name="eventBus"></param>
        public PublishConfirmationListener(IEventBus eventBus)
        {
            subscriptions = new[]
            {
                eventBus.Subscribe<MessageConfirmationEvent>(OnMessageConfirmation),
                eventBus.Subscribe<PublishChannelCreatedEvent>(OnPublishChannelCreated)
            };
        }

        /// <inheritdoc />
        public IPublishPendingConfirmation CreatePendingConfirmation(IModel model)
        {
            var deliveryTag = model.NextPublishSeqNo;
            var requests = unconfirmedChannelRequests.GetOrAdd(model.ChannelNumber, _ => new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
            var confirmationTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            requests.Add(deliveryTag, confirmationTcs);
            return new PublishPendingConfirmation(confirmationTcs);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach(var subscription in subscriptions)
                subscription.Dispose();
            InterruptAllUnconfirmedRequests(true);
        }

        private void OnMessageConfirmation(MessageConfirmationEvent @event)
        {
            if (!unconfirmedChannelRequests.TryGetValue(@event.Channel.ChannelNumber, out var requests)) return;

            var deliveryTag = @event.DeliveryTag;
            var multiple = @event.Multiple;
            var isNack = @event.IsNack;
            if (multiple)
            {
                foreach (var sequenceNumber in requests.Select(x => x.Key))
                    if (sequenceNumber <= deliveryTag && requests.TryRemove(sequenceNumber, out var confirmation))
                        Confirm(confirmation, sequenceNumber, isNack);
            }
            else if (requests.TryRemove(deliveryTag, out var confirmation))
            {
                Confirm(confirmation, deliveryTag, isNack);
            }
        }

        private void OnPublishChannelCreated(PublishChannelCreatedEvent @event)
        {
            InterruptAllUnconfirmedRequests();
            InitializeUnconfirmedRequests(@event.Channel);
        }

        private void InterruptAllUnconfirmedRequests(bool cancellationInsteadOfInterruption=false)
        {
            foreach (var channelNumber in unconfirmedChannelRequests.Select(x => x.Key))
            {
                if (!unconfirmedChannelRequests.TryRemove(channelNumber, out var requests)) continue;
                foreach (var deliveryTag in requests.Select(x => x.Key))
                {
                    if (!requests.TryRemove(deliveryTag, out var confirmationTcs)) continue;

                    if (cancellationInsteadOfInterruption)
                        confirmationTcs.TrySetCanceled();
                    else
                        confirmationTcs.TrySetException(new PublishInterruptedException());
                }
            }
        }

        private void InitializeUnconfirmedRequests(IModel model)
        {
            unconfirmedChannelRequests.Add(model.ChannelNumber, new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
        }

        private static void Confirm(TaskCompletionSource<object> tcs, ulong sequenceNumber, bool isNack)
        {
            if (isNack)
                tcs.TrySetException(new PublishNackedException($"Broker has signalled that publish {sequenceNumber} was unsuccessful"));
            else
                tcs.TrySetResult(null);
        }

        private class PublishPendingConfirmation : IPublishPendingConfirmation
        {
            private readonly TaskCompletionSource<object> confirmationTcs;

            public PublishPendingConfirmation(TaskCompletionSource<object> confirmationTcs)
            {
                this.confirmationTcs = confirmationTcs;
            }

            public Task WaitAsync(CancellationToken cancellationToken)
            {
                return TaskHelpers.WithCancellation(confirmationTcs.Task, cancellationToken);
            }
        }
    }
}
