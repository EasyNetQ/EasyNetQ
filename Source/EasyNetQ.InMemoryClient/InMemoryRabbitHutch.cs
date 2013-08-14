using EasyNetQ.Loggers;

namespace EasyNetQ.InMemoryClient
{
    public class InMemoryRabbitHutch
    {
        public InMemoryConnectionFactory ConnectionFactory { get; private set; }

        public IBus CreateBus()
        {
            ConnectionFactory = new InMemoryConnectionFactory();

            var serializer = new JsonSerializer();
            var logger = new ConsoleLogger();
            var conventions = new Conventions();
            var consumerErrorStrategy = new DefaultConsumerErrorStrategy(ConnectionFactory, serializer, logger, conventions);
            var messageValidationStrategy = new DefaultMessageValidationStrategy(logger, TypeNameSerializer.Serialize);

            var advancedBus = new RabbitAdvancedBus(
                new ConnectionConfiguration(),
                ConnectionFactory,
                TypeNameSerializer.Serialize,
                serializer,
                new QueueingConsumerFactory(logger, consumerErrorStrategy),
                logger,
                CorrelationIdGenerator.GetCorrelationId,
                conventions,
                messageValidationStrategy);

            return new RabbitBus(
                TypeNameSerializer.Serialize,
                logger,
                conventions,
                advancedBus);            
        }
    }
}