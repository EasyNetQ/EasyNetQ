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
            using (var scope = resolver.CreateScope())
            {
                var consumer = scope.Resolve<TConsumer>();
                consumer.Consume(message);
            }
        }

        public async Task DispatchAsync<TMessage, TAsyncConsumer>(TMessage message)
            where TMessage : class
            where TAsyncConsumer : class, IConsumeAsync<TMessage>
        {
            using (var scope = resolver.CreateScope())
            {
                var asyncConsumer = scope.Resolve<TAsyncConsumer>();
                await asyncConsumer.ConsumeAsync(message).ConfigureAwait(false);
            }
        }

        private class ActivatorBasedResolver : IServiceResolver
        {
            public TService Resolve<TService>() where TService : class
            {
                return Activator.CreateInstance<TService>();
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }
    }
}