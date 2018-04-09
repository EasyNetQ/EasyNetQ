using System;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumerFactory : IDisposable
    {
        IInternalConsumer CreateConsumer();
        void OnDisconnected();
    }

    public class InternalConsumerFactory : IInternalConsumerFactory
    {
        private readonly IHandlerRunner handlerRunner;
        private readonly IConventions conventions;
        private readonly IConsumerDispatcherFactory consumerDispatcherFactory;
        private readonly IEventBus eventBus;

        public InternalConsumerFactory(
            IHandlerRunner handlerRunner, 
            IConventions conventions, 
            IConsumerDispatcherFactory consumerDispatcherFactory, 
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(consumerDispatcherFactory, "consumerDispatcherFactory");

            this.handlerRunner = handlerRunner;
            this.conventions = conventions;
            this.consumerDispatcherFactory = consumerDispatcherFactory;
            this.eventBus = eventBus;
        }

        public IInternalConsumer CreateConsumer()
        {
            var dispatcher = consumerDispatcherFactory.GetConsumerDispatcher();
            return new InternalConsumer(handlerRunner, dispatcher, conventions, eventBus);
        }

        public void OnDisconnected()
        {
            consumerDispatcherFactory.OnDisconnected();
        }

        public void Dispose()
        {
            consumerDispatcherFactory.Dispose();
            handlerRunner.Dispose();
        }
    }
}