using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using EasyNetQ.DI;
using EasyNetQ.Producer;
using EasyNetQ.Scheduling;
using FluentAssertions;

namespace EasyNetQ.Tests.Mocking
{
    public class MockBuilder
    {
        private readonly IConnectionFactory connectionFactory = Substitute.For<IConnectionFactory>();
        private readonly IConnection connection = Substitute.For<IConnection>();
        private readonly List<IModel> channels = new List<IModel>();
        private readonly Stack<IModel> channelPool = new Stack<IModel>();
        private readonly List<IBasicConsumer> consumers = new List<IBasicConsumer>();
        private readonly IBasicProperties basicProperties = new BasicProperties();
        private readonly List<string> consumerQueueNames = new List<string>();
        private readonly IBus bus;

        public const string Host = "my_host";
        public const string VirtualHost = "my_virtual_host";
        public const int PortNumber = 1234;

        public MockBuilder() : this(register => { }) { }

        public MockBuilder(Action<IServiceRegister> registerServices) : this("host=localhost", registerServices) { }

        public MockBuilder(string connectionString) : this(connectionString, register => { }) { }

        public MockBuilder(string connectionString, Action<IServiceRegister> registerServices)
        {
            for (var i = 0; i < 10; i++)
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

        public IPubSub PubSub => bus.PubSub;

        public IRpc Rpc => bus.Rpc;
        
        public ISendReceive SendReceive => bus.SendReceive;
        
        public IScheduler Scheduler => bus.Scheduler;
        
        public IConnectionFactory ConnectionFactory => connectionFactory;

        public IConnection Connection => connection;

        public List<IModel> Channels => channels;

        public List<IBasicConsumer> Consumers => consumers;

        public IBasicProperties BasicProperties => basicProperties;

        public IBus Bus => bus;

        public IServiceResolver ServiceProvider => bus.Advanced.Container;

        public IModel NextModel => channelPool.Peek();

        public IEventBus EventBus => ServiceProvider.Resolve<IEventBus>();

        public List<string> ConsumerQueueNames => consumerQueueNames;
    }
}