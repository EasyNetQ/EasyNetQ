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
        private readonly IEasyNetQLogger logger;
        private readonly IConventions conventions;
        private readonly ConnectionConfiguration connectionConfiguration;
        private readonly IConsumerDispatcherFactory consumerDispatcherFactory;
        private readonly IEventBus eventBus;

        public InternalConsumerFactory(
            IHandlerRunner handlerRunner, 
            IEasyNetQLogger logger, 
            IConventions conventions, 
            ConnectionConfiguration connectionConfiguration, 
            IConsumerDispatcherFactory consumerDispatcherFactory, 
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(consumerDispatcherFactory, "consumerDispatcherFactory");

            this.handlerRunner = handlerRunner;
            this.logger = logger;
            this.conventions = conventions;
            this.connectionConfiguration = connectionConfiguration;
            this.consumerDispatcherFactory = consumerDispatcherFactory;
            this.eventBus = eventBus;
        }

        public IInternalConsumer CreateConsumer()
        {
            var dispatcher = consumerDispatcherFactory.GetConsumerDispatcher();
            return new InternalConsumer(handlerRunner, logger, dispatcher, conventions, connectionConfiguration, eventBus);
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