
using System;

namespace EasyNetQ.Consumer
{
    public class ConsumerCancellation : IDisposable
    {
       
        public delegate void ConsumerCancel(object queue);
        // This event will fire whenever this class is disposed. It is necessary in order to catch
        // when the consumer is canceled by the broker as well as the user. 
        public event ConsumerCancel ConsumerCancelled;

        private readonly Action onCancellation;

        public ConsumerCancellation(Action onCancellation)
        {
            Preconditions.CheckNotNull(onCancellation, "onCancellation");

            this.onCancellation = onCancellation;
        }

        public void Dispose()
        {
            onCancellation();
        }

        public void OnCancel(object queue)
        {
            ConsumerCancelled?.Invoke(queue);
        }
    }
}