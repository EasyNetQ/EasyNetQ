using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public class PublishConfirmationWaiter : IPublishConfirmationWaiter
    {
        private readonly Action action;
        private readonly CancellationToken cancellation;
        private readonly Task confirmation;
        private readonly ulong deliveryTag;

        public PublishConfirmationWaiter(ulong deliveryTag, Task confirmation, CancellationToken cancellation, Action action)
        {
            this.deliveryTag = deliveryTag;
            this.confirmation = confirmation;
            this.cancellation = cancellation;
            this.action = action;
        }

        public async Task WaitAsync(TimeSpan timeout)
        {
            try
            {
                using (var timeoutCancellation = new CancellationTokenSource())
                {
                    using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellation.Token, cancellation))
                    {
                        var timeoutTask = Task.Delay(timeout, compositeCancellation.Token);
                        if (timeoutTask == await Task.WhenAny(confirmation, timeoutTask).ConfigureAwait(false)) throw new TimeoutException(string.Format("Publisher confirms timed out after {0} seconds waiting for ACK or NACK from sequence number {1}", (int) timeout.TotalSeconds, deliveryTag));
                        timeoutCancellation.Cancel();
                        await confirmation.ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                action();
            }
        }

        public void Cancel()
        {
            action();
        }
    }
}