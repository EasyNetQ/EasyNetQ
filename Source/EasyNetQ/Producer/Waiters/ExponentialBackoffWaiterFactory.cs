using System.Threading;

namespace EasyNetQ.Producer.Waiters
{
    public class ExponentialBackoffWaiterFactory : IReconnectionWaiterFactory
    {
        private const int DefaultInitialDelayInMilliseconds = 10;
        private readonly int initialDelayInMilliseconds;

        public ExponentialBackoffWaiterFactory()
        {
            initialDelayInMilliseconds = DefaultInitialDelayInMilliseconds;
        }

        public IReconnectionWaiter GetWaiter()
        {
            return new ExponentialBackoffWaiter(initialDelayInMilliseconds);
        }

        private class ExponentialBackoffWaiter : IReconnectionWaiter
        {
            private int delayInMilliseconds;

            public ExponentialBackoffWaiter(int initialDelayInMilliseconds)
            {
                delayInMilliseconds = initialDelayInMilliseconds;
            }

            public void Wait()
            {
                Thread.Sleep(delayInMilliseconds);
                delayInMilliseconds *= 2;
            }
        }
    }
}