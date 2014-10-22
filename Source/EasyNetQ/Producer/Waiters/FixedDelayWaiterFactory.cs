using System.Threading;

namespace EasyNetQ.Producer.Waiters
{
    public class FixedDelayWaiterFactory : IReconnectionWaiterFactory
    {
        private const int DefaultDelayInMilliseconds = 1000;

        private readonly int delayInMilliseconds;

        public FixedDelayWaiterFactory(int delayInMilliseconds)
        {
            this.delayInMilliseconds = delayInMilliseconds;
        }

        public FixedDelayWaiterFactory() : this(DefaultDelayInMilliseconds)
        {
        }

        public IReconnectionWaiter GetWaiter()
        {
            return new FixedDelayWaiter(delayInMilliseconds);
        }

        private class FixedDelayWaiter : IReconnectionWaiter
        {
            private readonly int delayInMilliseconds;

            public FixedDelayWaiter(int delayInMilliseconds)
            {
                this.delayInMilliseconds = delayInMilliseconds;
            }

            public void Wait()
            {
                Thread.Sleep(delayInMilliseconds);
            }
        }
    }
}