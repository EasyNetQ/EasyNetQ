using System;
using System.Collections.Generic;
using EasyNetQ.DI;
using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.Mocking
{
    public class MockBuilder
    {
        private readonly IBasicProperties basicProperties = new BasicProperties();
        private readonly IBus bus;
        private readonly Stack<IModel> channelPool = new Stack<IModel>();
        private readonly List<IModel> channels = new List<IModel>();
        private readonly IConnection connection = Substitute.For<IAutorecoveringConnection>();
        private readonly IConnectionFactory connectionFactory = Substitute.For<IConnectionFactory>();
        private readonly List<string> consumerQueueNames = new List<string>();
        private readonly List<IBasicConsumer> consumers = new List<IBasicConsumer>();

        public MockBuilder() : this(register => { })
        {
        }

        public MockBuilder(Action<IServiceRegister> registerServices) : this("host=localhost", registerServices)
        {
        }

        public MockBuilder(string connectionString) : this(connectionString, register => { })
        {
        }

        public MockBuilder(string connectionString, Action<IServiceRegister> registerServices)
        {
            for (var i = 0; i < 10; i++)
            {
                channelPool.Push(Substitute.For<IModel, IRecoverable>());
            }

            connectionFactory.CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>()).Returns(connection);
            connection.IsOpen.Returns(true);
            connection.Endpoint.Returns(new AmqpTcpEndpoint("localhost"));

            connection.CreateModel().Returns(i =>
            {
                var channel = channelPool.Pop();
                channels.Add(channel);
                channel.CreateBasicProperties().Returns(basicProperties);
                channel.IsOpen.Returns(true);
                channel.BasicConsume(null, false, null, true, false, null, null)
                    .ReturnsForAnyArgs(consumeInvocation =>
                    {
                        var queueName = (string)consumeInvocation[0];
                        var consumerTag = (string)consumeInvocation[2];
                        var consumer = (IBasicConsumer)consumeInvocation[6];

                        ConsumerQueueNames.Add(queueName);
                        consumer.HandleBasicConsumeOk(consumerTag);
                        consumers.Add(consumer);
                        return string.Empty;
                    });
                channel.QueueDeclare(null, true, false, false, null)
                    .ReturnsForAnyArgs(queueDeclareInvocation =>
                    {
                        var queueName = (string)queueDeclareInvocation[0];

                        return new QueueDeclareOk(queueName, 0, 0);
                    });

                return channel;
            });

            bus = RabbitHutch.CreateBus(connectionString, x =>
            {
                registerServices(x);
                x.Register(connectionFactory);
            });

            bus.Should().NotBeNull();
            bus.Advanced.Should().NotBeNull();
            bus.Advanced.Container.Should().NotBeNull();
        }

        public IPubSub PubSub => bus.PubSub;

        public IRpc Rpc => bus.Rpc;

        public ISendReceive SendReceive => bus.SendReceive;

        public IScheduler Scheduler => bus.Scheduler;

        public IConnectionFactory ConnectionFactory => connectionFactory;

        public IConnection Connection => connection;

        public List<IModel> Channels => channels;

        public List<IBasicConsumer> Consumers => consumers;

        public IBus Bus => bus;

        public IServiceResolver ServiceProvider => bus.Advanced.Container;

        public IModel NextModel => channelPool.Peek();

        public IEventBus EventBus => ServiceProvider.Resolve<IEventBus>();

        public IPersistentConnection PersistentConnection => ServiceProvider.Resolve<IPersistentConnection>();

        public List<string> ConsumerQueueNames => consumerQueueNames;
    }
}
