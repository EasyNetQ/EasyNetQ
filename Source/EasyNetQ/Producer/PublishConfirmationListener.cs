using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public class PublishConfirmationListener : IPublishConfirmationListener
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly ConcurrentDictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<object>>> unconfirmedChannelRequests = new ConcurrentDictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<object>>>();

        public PublishConfirmationListener(IEventBus eventBus)
        {
            eventBus.Subscribe<MessageConfirmationEvent>(OnMessageConfirmation);
            eventBus.Subscribe<PublishChannelCreatedEvent>(OnPublishChannelCreated);
        }

        public IPublishConfirmationWaiter GetWaiter(IModel model)
        {
            var deliveryTag = model.NextPublishSeqNo;
            var requests = unconfirmedChannelRequests.GetOrAdd(model, _ => new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
            var confirmation = TaskHelpers.CreateTcs<object>();
            requests.Add(deliveryTag, confirmation);
            return new PublishConfirmationWaiter(deliveryTag, confirmation.Task, cancellation.Token, () => requests.Remove(deliveryTag));
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }

        private void OnMessageConfirmation(MessageConfirmationEvent @event)
        {
            if (!unconfirmedChannelRequests.TryGetValue(@event.Channel, out var requests)) return;

            var deliveryTag = @event.DeliveryTag;
            var multiple = @event.Multiple;
            var isNack = @event.IsNack;
            if (multiple)
            {
                foreach (var sequenceNumber in requests.Keys.Where(x => x <= deliveryTag))
                    if (requests.TryGetValue(sequenceNumber, out var confirmation))
                        Confirm(confirmation, sequenceNumber, isNack);
            }
            else if (requests.TryGetValue(deliveryTag, out var confirmation))
            {
                Confirm(confirmation, deliveryTag, isNack);
            }
        }

        private void OnPublishChannelCreated(PublishChannelCreatedEvent @event)
        {
            foreach (var channel in unconfirmedChannelRequests.Keys)
            {
                if (!unconfirmedChannelRequests.TryRemove(channel, out var confirmations)) continue;
                foreach (var deliveryTag in confirmations.Keys)
                {
                    if (!confirmations.TryRemove(deliveryTag, out var confirmation)) continue;

                    confirmation.TrySetExceptionAsynchronously(new PublishInterruptedException());
                }
            }

            unconfirmedChannelRequests.Add(@event.Channel, new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
        }

        private static void Confirm(TaskCompletionSource<object> tcs, ulong deliveryTag, bool isNack)
        {
            if (isNack)
                tcs.TrySetExceptionAsynchronously(new PublishNackedException(string.Format("Broker has signalled that publish {0} was unsuccessful", deliveryTag)));
            else
                tcs.TrySetResultAsynchronously(null);
        }
    }
}
