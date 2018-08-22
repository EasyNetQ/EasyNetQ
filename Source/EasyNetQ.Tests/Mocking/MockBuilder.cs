using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using EasyNetQ.DI;
using FluentAssertions;

namespace EasyNetQ.Tests.Mocking
{
    public class MockBuilder
    {
        readonly IConnectionFactory connectionFactory = Substitute.For<IConnectionFactory>();
        readonly IConnection connection = Substitute.For<IConnection>();
        readonly List<IModel> channels = new List<IModel>();
        readonly Stack<IModel> channelPool = new Stack<IModel>();
        readonly List<IBasicConsumer> consumers = new List<IBasicConsumer>();
        readonly IBasicProperties basicProperties = new BasicProperties();
        readonly List<string> consumerQueueNames = new List<string>();
        private readonly IBus bus;

        public const string Host = "my_host";
        public const string VirtualHost = "my_virtual_host";
        public const int PortNumber = 1234;

        public MockBuilder() : this(register => { }) { }

        public MockBuilder(Action<IServiceRegister> registerServices) : this("host=localhost", registerServices) { }

        public MockBuilder(string connectionString) : this(connectionString, register => { }) { }

        public MockBuilder(string connectionString, Action<IServiceRegister> registerServices)
        {
            for (int i = 0; i < 10; i++)
            {
                channelPool.Push(Substitute.For<IModel>());
            }

            connectionFactory.CreateConnection().Returns(connection);
            connectionFactory.Next().Returns(false);
            connectionFactory.Succeeded.Returns(true);
            connectionFactory.CurrentHost.Returns(new HostConfiguration
            {
                Host = Host,
                Port = PortNumber
            });
            connectionFactory.Configuration.Returns(new ConnectionConfiguration
            {
                VirtualHost = VirtualHost,
            });

            connection.IsOpen.Returns(true);

            connection.CreateModel().Returns(i =>
            {
                var channel = channelPool.Pop();
                channels.Add(channel);
                channel.CreateBasicProperties().Returns(basicProperties);
                channel.IsOpen.Returns(true);
                channel.BasicConsume(null, false, null, true, false, null, null)
                    .ReturnsForAnyArgs(consumeInvokation =>
                    {
                        var queueName = (string)consumeInvokation[0];
                        var consumerTag = (string)consumeInvokation[2];
                        var consumer = (IBasicConsumer)consumeInvokation[6];

                        ConsumerQueueNames.Add(queueName);
                        consumer.HandleBasicConsumeOk(consumerTag);
                        consumers.Add(consumer);
                        return string.Empty;
                    });
                channel.QueueDeclare(null, true, false, false, null)
                    .ReturnsForAnyArgs(queueDeclareInvocation =>
                    {
                        var queueName = (string) queueDeclareInvocation[0];

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

        public IConnectionFactory ConnectionFactory
        {
            get { return connectionFactory; }
        }

        public IConnection Connection
        {
            get { return connection; }
        }

        public List<IModel> Channels
        {
            get { return channels; }
        }

        public List<IBasicConsumer> Consumers
        {
            get { return consumers; }
        }

        public IBasicProperties BasicProperties
        {
            get { return basicProperties; }
        }

        public IBus Bus
        {
            get { return bus; }
        }

        public IServiceResolver ServiceProvider
        {
            get { return bus.Advanced.Container; }
        }

        public IModel NextModel
        {
            get { return channelPool.Peek(); }
        }

        public IEventBus EventBus
        {
            get { return ServiceProvider.Resolve<IEventBus>(); }
        }

        public List<string> ConsumerQueueNames
        {
            get { return consumerQueueNames; }
        }
    }
}