using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public class PublishConfirmationListener : IPublishConfirmationListener
    {
        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<NullStruct>> unconfirmedRequests = new ConcurrentDictionary<ulong, TaskCompletionSource<NullStruct>>();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        public PublishConfirmationListener(IEventBus eventBus)
        {
            eventBus.Subscribe<MessageConfirmationEvent>(OnMessageConfirmation);
        }

        public void Request(ulong deliveryTag)
        {
            unconfirmedRequests.Add(deliveryTag, new TaskCompletionSource<NullStruct>());
        }

        public void Discard(ulong deliveryTag)
        {
            unconfirmedRequests.Remove(deliveryTag);
        }

        public void Wait(ulong sequenceNumber, TimeSpan timeout)
        {
            var confirmation = unconfirmedRequests[sequenceNumber];
            try
            {
                var task = confirmation.Task;
                if (task.Wait((int) timeout.TotalSeconds, cancellation.Token))
                {
                    return;
                }

                throw new TimeoutException(string.Format("Publisher confirms timed out after {0} seconds waiting for ACK or NACK from sequence number {1}", 10, sequenceNumber));
            }
            finally
            {
                unconfirmedRequests.Remove(sequenceNumber);
            }
        }

        public async Task WaitAsync(ulong sequenceNumber, TimeSpan timeout)
        {
            var confirmation = unconfirmedRequests[sequenceNumber];
            var timeoutTask = TaskHelpers.Timeout(timeout, cancellation.Token);
            if (timeoutTask == await TaskHelpers.WhenAny(confirmation.Task, timeoutTask).ConfigureAwait(false))
            {
                throw new TimeoutException(string.Format("Publisher confirms timed out after {0} seconds waiting for ACK or NACK from sequence number {1}", 10, sequenceNumber));
            }
            await confirmation.Task.ConfigureAwait(false);
        }

        private void OnMessageConfirmation(MessageConfirmationEvent @event)
        {
            var deliveryTag = @event.DeliveryTag;
            var multiple = @event.Multiple;
            var isNack = @event.IsNack;

            if (multiple)
            {
                // Fix me: Where(x <= deliveryTag) is O(N) operation and could perform slowly.
                // Fix me: ConcurrentDictionary.Keys acquires all locks, it is very expensive operation and could perform slowly.
                foreach (var sequenceNumber in unconfirmedRequests.Keys.Where(x => x <= deliveryTag))
                {
                    TaskCompletionSource<NullStruct> confirmation;
                    if (unconfirmedRequests.TryGetValue(sequenceNumber, out confirmation))
                    {
                        Confirm(confirmation, sequenceNumber, isNack);
                    }
                }
            }
            else
            {
                TaskCompletionSource<NullStruct> confirmation;
                if (unconfirmedRequests.TryGetValue(deliveryTag, out confirmation))
                {
                    Confirm(confirmation, deliveryTag, isNack);
                }
            }
        }

        private static void Confirm(TaskCompletionSource<NullStruct> tcs, ulong sequenceNumber, bool isNack)
        {
            if (isNack)
            {
                tcs.TrySetExceptionSafe(new PublishNackedException(string.Format("Broker has signalled that publish {0} was unsuccessful", sequenceNumber)));
            }
            else
            {
                tcs.TrySetResultSafe(new NullStruct());
            }
        }

        private struct NullStruct
        {
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }
    }
}