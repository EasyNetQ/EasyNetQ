namespace EasyNetQ.Producer.Waiters
{
    public interface IReconnectionWaiterFactory
    {
        IReconnectionWaiter GetWaiter();
    }
}