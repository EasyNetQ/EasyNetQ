using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;

namespace EasyNetQ.Producer
{
    public class PublishConfirmationListener : IPublishConfirmationListener
    {
        private ConcurrentDictionary<ulong, TaskCompletionSource<object>> unconfirmedRequests = new ConcurrentDictionary<ulong, TaskCompletionSource<object>>();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        public PublishConfirmationListener(IEventBus eventBus)
        {
            eventBus.Subscribe<MessageConfirmationEvent>(OnMessageConfirmation);
            eventBus.Subscribe<PublishChannelCreatedEvent>(OnPublishChannelCreated);
        }

        public IPublishConfirmationWaiter GetWaiter(ulong deliveryTag)
        {
            var requests = unconfirmedRequests;
            var confirmation = new TaskCompletionSource<object>();
            requests.Add(deliveryTag, confirmation);
            return new PublishConfirmationWaiter(deliveryTag, confirmation.Task, cancellation.Token, () => requests.Remove(deliveryTag));
        }

        private void OnMessageConfirmation(MessageConfirmationEvent @event)
        {
            var requests = unconfirmedRequests;
            var deliveryTag = @event.DeliveryTag;
            var multiple = @event.Multiple;
            var isNack = @event.IsNack;
            if (multiple)
            {
                // Fix me: ConcurrentDictionary.Keys acquires all locks, it is very expensive operation and could perform slowly.
                foreach (var sequenceNumber in requests.Keys.Where(x => x <= deliveryTag))
                {
                    TaskCompletionSource<object> confirmation;
                    if (requests.TryRemove(sequenceNumber, out confirmation))
                    {
                        Confirm(confirmation, sequenceNumber, isNack);
                    }
                }
            }
            else
            {
                TaskCompletionSource<object> confirmation;
                if (requests.TryRemove(deliveryTag, out confirmation))
                {
                    Confirm(confirmation, deliveryTag, isNack);
                }
            }
        }

        private void OnPublishChannelCreated(PublishChannelCreatedEvent @event)
        {
            var unconfirmedRequestsToCancel = Interlocked.Exchange(ref unconfirmedRequests, new ConcurrentDictionary<ulong, TaskCompletionSource<object>>());
            foreach (var unconfirmedRequestToCancel in unconfirmedRequestsToCancel.Values)
            {
                unconfirmedRequestToCancel.TrySetExceptionSafe(new PublishInterruptedException());
            }
        }

        private static void Confirm(TaskCompletionSource<object> tcs, ulong deliveryTag, bool isNack)
        {
            if (isNack)
            {
                tcs.TrySetExceptionSafe(new PublishNackedException(string.Format("Broker has signalled that publish {0} was unsuccessful", deliveryTag)));
            }
            else
            {
                tcs.TrySetResultSafe(null);
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }
    }
}