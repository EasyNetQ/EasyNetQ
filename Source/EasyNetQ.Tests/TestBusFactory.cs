using System;
using EasyNetQ.Loggers;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class TestBusFactory
    {
        public IEasyNetQLogger Logger { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public ISerializer Serializer { get; set; }
        public IConsumerFactory ConsumerFactory { get; set; }
        public IConsumerErrorStrategy ConsumerErrorStrategy { get; set; }
        public IConnection Connection { get; set; }
        public IModel Model { get; set; }
        public Func<string> GetCorrelationId { get; set; }
        public IConventions Conventions { get; set; }

        public IBus CreateBusWithMockAmqpClient()
        {
            Logger = Logger ?? new ConsoleLogger();
            Model = Model ?? new MockModel();
            Connection = Connection ?? new MockConnection(Model);
            ConnectionFactory = ConnectionFactory ?? new MockConnectionFactory(Connection);
            Serializer = Serializer ?? new JsonSerializer();
            ConsumerErrorStrategy = ConsumerErrorStrategy ?? new DefaultConsumerErrorStrategy(ConnectionFactory, Serializer, Logger);
            ConsumerFactory = ConsumerFactory ?? new QueueingConsumerFactory(Logger, ConsumerErrorStrategy);
            GetCorrelationId = GetCorrelationId ?? CorrelationIdGenerator.GetCorrelationId;
            Conventions = Conventions ?? new Conventions();

            var advancedBus = new RabbitAdvancedBus(
                new ConnectionConfiguration(), 
                ConnectionFactory,
                TypeNameSerializer.Serialize,
                Serializer,
                ConsumerFactory,
                Logger,
                GetCorrelationId,
                Conventions);
            
            return new RabbitBus(
                TypeNameSerializer.Serialize,
                Logger,
                Conventions,
                advancedBus);
        }
    }
}