using System;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_using_default_names_provider
    {
        private INamesProvider namesProvider;

        [SetUp]
        public void SetUp()
        {
            namesProvider = new DefaultNamesProvider();
        }

        [Test]
        public void The_default_errorQueue_name_should_be()
        {
            var result = namesProvider.EasyNetQErrorQueue;
            result.ShouldEqual("EasyNetQ_Default_Error_Queue");
        }

        [Test]
        public void The_default_errorExchange_name_should_be()
        {
            var result = namesProvider.ErrorExchangePrefix;
            result.ShouldEqual("ErrorExchange_");
        }

        [Test]
        public void The_default_errorRpc_name_should_be()
        {
            var result = namesProvider.RpcExchange;
            result.ShouldEqual("easy_net_q_rpc");
        }
    }

    [TestFixture]
    public class When_using_custom_names_provider_and_registering_respons_handler
    {
        private RabbitBus bus;
        private string createdExchangeName;

        [SetUp]
        public void SetUp()
        {
            var mockModel = new MockModel
                                {
                                    ExchangeDeclareAction = (exchangeName, type, durable, autoDelete, arguments) => createdExchangeName = exchangeName,
                                    BasicConsumeAction = (queue, noAck, consumerTag, consumer) => { return string.Empty; }
                                };

            var namesProvider = new MockCustomNamesProvider();

            CreateBus(namesProvider, mockModel);
            bus.Respond<TestMessage, TestMessage>(t => { return new TestMessage(); });
        }

        private void CreateBus(INamesProvider namesProvider, IModel model)
        {
            var advancedBus = new RabbitAdvancedBus(
                new ConnectionConfiguration(),
                new MockConnectionFactory(new MockConnection(model)),
                TypeNameSerializer.Serialize,
                new JsonSerializer(),
                new MockConsumerFactory(),
                new MockLogger(),
                CorrelationIdGenerator.GetCorrelationId,
                new Conventions()
                );

            bus = new RabbitBus(
                x => TypeNameSerializer.Serialize(x.GetType()),
                new MockLogger(),
                new Conventions(),
                namesProvider,
                advancedBus
                );
        }

        [Test]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            createdExchangeName.ShouldEqual("CustomRpcExchangeName");
        }
    }

    [TestFixture]
    public class When_using_custom_names_provider_with_default_consumer_error_strategy
    {
        private DefaultConsumerErrorStrategy errorStrategy;
        private string createdExchangeName;
        private string createdQueueName;

        [SetUp]
        public void SetUp()
        {
            var mockModel = new MockModel
            {
                ExchangeDeclareAction = (exchangeName, type, durable, autoDelete, arguments) => createdExchangeName = exchangeName,
                QueueDeclareAction = (queue, durable, exclusive, autoDelete, arguments) =>
                {
                    createdQueueName = queue;
                    return new QueueDeclareOk(queue, 0, 0);
                }
            };

            errorStrategy = new DefaultConsumerErrorStrategy(new MockConnectionFactory(new MockConnection(mockModel)), new JsonSerializer(), new MockLogger(), new MockCustomNamesProvider());

            var basicDeliverEventArgs = new BasicDeliverEventArgs();
            basicDeliverEventArgs.RoutingKey = "RoutingKey";
            errorStrategy.HandleConsumerError(basicDeliverEventArgs, new Exception());
        }

        [Test]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            createdExchangeName.ShouldEqual("CustomErrorExchangePrefixName_RoutingKey");
        }

        [Test]
        public void Should_use_queue_name_from_custom_names_provider()
        {
            createdQueueName.ShouldEqual("CustomEasyNetQErrorQueueName");
        }
    }
}