using System;
using System.Threading.Tasks;
using EasyNetQ.DI;

namespace EasyNetQ.AutoSubscribe
{
    public class DefaultAutoSubscriberMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly IServiceResolver resolver;

        public DefaultAutoSubscriberMessageDispatcher(IServiceResolver resolver)
        {
            this.resolver = resolver;
        }

        public DefaultAutoSubscriberMessageDispatcher()
            : this(new ActivatorBasedResolver())
        {   
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message) 
            where TMessage : class
            where TConsumer : class, IConsume<TMessage>
        {
            var consumer = resolver.Resolve<TConsumer>();
            consumer.Consume(message);
        }

        public async Task DispatchAsync<TMessage, TAsyncConsumer>(TMessage message)
            where TMessage : class
            where TAsyncConsumer : class, IConsumeAsync<TMessage>
        {
            var asynConsumer = resolver.Resolve<TAsyncConsumer>();
            await asynConsumer.ConsumeAsync(message).ConfigureAwait(false);
        }

        private class ActivatorBasedResolver : IServiceResolver
        {
            public TService Resolve<TService>() where TService : class
            {
                return Activator.CreateInstance<TService>();
            }
        }
    }
}