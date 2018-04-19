using System;
using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public class LightInjectMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly IServiceContainer container;

        public LightInjectMessageDispatcher(IServiceContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            container.GetInstance<TConsumer>().Consume(message);
        }

        public async Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            await container.GetInstance<TConsumer>().Consume(message).ConfigureAwait(false);
        }
    }
}
