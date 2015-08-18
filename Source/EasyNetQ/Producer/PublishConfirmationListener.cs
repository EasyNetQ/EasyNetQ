using System;
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

        public void Request(ulong deliveryTag)
        {
            unconfirmedRequests.Add(deliveryTag, new TaskCompletionSource<object>());
        }

        public void Cancel(ulong deliveryTag)
        {
            unconfirmedRequests.Remove(deliveryTag);
        }

        public void Wait(ulong deliveryTag, TimeSpan timeout)
        {
            TaskCompletionSource<object> confirmation;
            if (! unconfirmedRequests.TryGetValue(deliveryTag, out confirmation))
            {
                throw new OperationCanceledException();
            }

            try
            {
                if (confirmation.Task.Wait((int) timeout.TotalMilliseconds, cancellation.Token))
                {
                    return;
                }

                throw new TimeoutException(string.Format("Publisher confirms timed out after {0} seconds waiting for ACK or NACK from sequence number {1}", (int) timeout.TotalSeconds, deliveryTag));
            }
            catch (AggregateException exception)
            {
                throw exception.InnerException;
            }
            finally
            {
                unconfirmedRequests.Remove(deliveryTag);
            }
        }

        public async Task WaitAsync(ulong deliveryTag, TimeSpan timeout)
        {
            TaskCompletionSource<object> confirmation;
            if (!unconfirmedRequests.TryGetValue(deliveryTag, out confirmation))
            {
                throw new OperationCanceledException();
            }
            try
            {
                using (var timeoutCancellation = new CancellationTokenSource())
                {
                    using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellation.Token, cancellation.Token))
                    {
                        var timeoutTask = TaskHelpers.Delay(timeout, compositeCancellation.Token);
                        if (timeoutTask == await TaskHelpers.WhenAny(confirmation.Task, timeoutTask).ConfigureAwait(false))
                        {
                            throw new TimeoutException(string.Format("Publisher confirms timed out after {0} seconds waiting for ACK or NACK from sequence number {1}", (int) timeout.TotalSeconds, deliveryTag));
                        }
                        timeoutCancellation.Cancel();
                        await confirmation.Task.ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                unconfirmedRequests.Remove(deliveryTag);
            }
        }

        private void OnMessageConfirmation(MessageConfirmationEvent @event)
        {
            var deliveryTag = @event.DeliveryTag;
            var multiple = @event.Multiple;
            var isNack = @event.IsNack;

            if (multiple)
            {
                // Fix me: ConcurrentDictionary.Keys acquires all locks, it is very expensive operation and could perform slowly.
                foreach (var sequenceNumber in unconfirmedRequests.Keys.Where(x => x <= deliveryTag))
                {
                    TaskCompletionSource<object> confirmation;
                    if (unconfirmedRequests.TryGetValue(sequenceNumber, out confirmation))
                    {
                        Confirm(confirmation, sequenceNumber, isNack);
                    }
                }
            }
            else
            {
                TaskCompletionSource<object> confirmation;
                if (unconfirmedRequests.TryGetValue(deliveryTag, out confirmation))
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
                unconfirmedRequestToCancel.TrySetCanceledSafe();
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