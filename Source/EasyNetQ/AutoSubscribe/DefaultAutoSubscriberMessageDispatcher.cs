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
            var scope = resolver.CreateScope();
            try
            {
                var consumer = scope.Resolve<TConsumer>();
                consumer.Consume(message);
            }
            finally
            {
                if (scope is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public async Task DispatchAsync<TMessage, TAsyncConsumer>(TMessage message)
            where TMessage : class
            where TAsyncConsumer : class, IConsumeAsync<TMessage>
        {
            var scope = resolver.CreateScope();
            try
            {
                var asynConsumer = scope.Resolve<TAsyncConsumer>();
                await asynConsumer.ConsumeAsync(message).ConfigureAwait(false);
            }
            finally
            {
                if (scope is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private class ActivatorBasedResolver : IServiceResolver
        {
            public TService Resolve<TService>() where TService : class
            {
                return Activator.CreateInstance<TService>();
            }

            public IServiceResolver CreateScope()
            {
                return this;
            }
        }
    }
}