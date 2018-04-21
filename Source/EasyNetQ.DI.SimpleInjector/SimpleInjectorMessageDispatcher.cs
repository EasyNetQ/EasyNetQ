using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector
{
    public class SimpleInjectorMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly Container container;

        public SimpleInjectorMessageDispatcher(Container container)
        {
            this.container = container;
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            var instance = (IConsume<TMessage>)container.GetInstance(typeof(TConsumer));
            instance.Consume(message);
        }

        public async Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            var instance = (IConsumeAsync<TMessage>)container.GetInstance(typeof(TConsumer));
            await instance.Consume(message).ConfigureAwait(false);
        }
    }
}
