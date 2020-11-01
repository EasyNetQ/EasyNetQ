using System.Threading;

namespace EasyNetQ.AutoSubscribe
{
    public interface IConsume<in T> where T : class
    {
        void Consume(T message, CancellationToken cancellationToken = default);
    }
}
