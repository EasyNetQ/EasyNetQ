using EasyNetQ.Loggers;

namespace EasyNetQ.Tests
{
    public class TestBusFactory
    {
        public IEasyNetQLogger Logger { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public ISerializer Serializer { get; set; }
        public IConsumerFactory ConsumerFactory { get; set; }
        public IConsumerErrorStrategy ConsumerErrorStrategy { get; set; }

        public TestBusFactory()
        {
            Logger = new ConsoleLogger();
            ConnectionFactory = new MockConnectionFactory();
            Serializer = new JsonSerializer();
            ConsumerErrorStrategy = new DefaultConsumerErrorStrategy(ConnectionFactory, Serializer, Logger);
            ConsumerFactory = new QueueingConsumerFactory(Logger, ConsumerErrorStrategy);
        }

        public IBus CreateBusWithMockAmqpClient()
        {
            return new RabbitBus(
                TypeNameSerializer.Serialize,
                Serializer, 
                ConsumerFactory,
                ConnectionFactory, 
                Logger);
        }
    }
}