using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.AutoSubscribe
{
    public interface IAutoSubscriberMessageDispatcher
    {
        void Dispatch<TMessage, TConsumer>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class
            where TConsumer : class, IConsume<TMessage>;

        Task DispatchAsync<TMessage, TConsumer>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class
            where TConsumer : class, IConsumeAsync<TMessage>;
    }
}
