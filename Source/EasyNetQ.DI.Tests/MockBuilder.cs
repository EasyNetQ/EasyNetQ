using NSubstitute;
using Xunit;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;

namespace EasyNetQ.Tests.Mocking
{
    public class MockBuilder
    {
        readonly Stack<IModel> channelPool = new Stack<IModel>();

        public const string Host = "my_host";
        public const string VirtualHost = "my_virtual_host";
        public const int PortNumber = 1234;

        public IConnectionFactory ConnectionFactory { get; } = Substitute.For<IConnectionFactory>();
        public IConnection Connection { get; } = Substitute.For<IConnection>();
        public List<IModel> Channels { get; } = new List<IModel>();
        public List<IBasicConsumer> Consumers { get; } = new List<IBasicConsumer>();
        public IBasicProperties BasicProperties { get; } = new BasicProperties();
        public List<string> ConsumerQueueNames { get; } = new List<string>();
        public IBus Bus { get; private set; }
        public IServiceProvider ServiceProvider
        {
            get { return Bus.Advanced.Container; }
        }

        public IModel NextModel
        {
            get { return channelPool.Peek(); }
        }

        public IEventBus EventBus
        {
            get { return ServiceProvider.Resolve<IEventBus>(); }
        }

        public MockBuilder() : this(register => { }) { }

        public MockBuilder(Action<IServiceRegister> registerServices) : this("host=localhost", registerServices) { }

        public MockBuilder(string connectionString) : this(connectionString, register => { }) { }

        public MockBuilder(string connectionString, Action<IServiceRegister> registerServices)
        {
            for (int i = 0; i < 10; i++)
            {
                channelPool.Push(Substitute.For<IModel>());
            }

            ConnectionFactory.CreateConnection().Returns(Connection);
            ConnectionFactory.Next().Returns(false);
            ConnectionFactory.Succeeded.Returns(true);
            ConnectionFactory.CurrentHost.Returns(new HostConfiguration
            {
                Host = Host,
                Port = PortNumber
            });
            ConnectionFactory.Configuration.Returns(new ConnectionConfiguration
            {
                VirtualHost = VirtualHost,
            });

            Connection.IsOpen.Returns(true);

            Connection.CreateModel().Returns(i =>
            {
                var channel = channelPool.Pop();
                Channels.Add(channel);
                channel.CreateBasicProperties().Returns(BasicProperties);
                channel.IsOpen.Returns(true);
                channel.BasicConsume(null, false, null, true, false, null, null)
                    .ReturnsForAnyArgs(consumeInvokation =>
                    {
                        var queueName = (string)consumeInvokation[0];
                        var consumerTag = (string)consumeInvokation[2];
                        var consumer = (IBasicConsumer)consumeInvokation[6];

                        ConsumerQueueNames.Add(queueName);
                        consumer.HandleBasicConsumeOk(consumerTag);
                        Consumers.Add(consumer);
                        return string.Empty;
                    });

                return channel;
            });

            Bus = RabbitHutch.CreateBus(connectionString, x =>
            {
                registerServices(x);
                x.Register(_ => ConnectionFactory);
            });

            Assert.NotNull(Bus);
            Assert.NotNull(Bus.Advanced);
            Assert.NotNull(Bus.Advanced.Container);
        }

    }
}