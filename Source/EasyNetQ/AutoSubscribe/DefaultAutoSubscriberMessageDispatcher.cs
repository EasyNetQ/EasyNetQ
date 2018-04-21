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
            : this(new ReflectionBasedResolver())
        {   
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message) 
            where TMessage : class
            where TConsumer : class, IConsume<TMessage>
        {
            resolver.Resolve<TConsumer>().Consume(message);
        }

        public async Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : class, IConsumeAsync<TMessage>
        {
            await resolver.Resolve<TConsumer>().ConsumeAsync(message).ConfigureAwait(false);
        }

        private class ReflectionBasedResolver : IServiceResolver
        {
            public TService Resolve<TService>() where TService : class
            {
                return ReflectionHelpers.CreateInstance<TService>();
            }
        }
    }
}