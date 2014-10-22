namespace EasyNetQ.Producer.Waiters
{
    public interface IReconnectionWaiter
    {
        void Wait();
    }
}