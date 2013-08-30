using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Mocking
{
    public class MockBuilder
    {
        readonly IConnectionFactory connectionFactory = MockRepository.GenerateStub<IConnectionFactory>();
        readonly IConnection connection = MockRepository.GenerateStub<IConnection>();
        readonly List<IModel> channels = new List<IModel>();
        readonly List<IBasicConsumer> consumers = new List<IBasicConsumer>(); 
        readonly IBasicProperties basicProperties = new BasicProperties();
        private readonly IEasyNetQLogger logger = MockRepository.GenerateStub<IEasyNetQLogger>();
        private readonly IBus bus;
        private IServiceProvider serviceProvider;

        public const string Host = "my_host";
        public const string VirtualHost = "my_virtual_host";
        public const int PortNumber = 1234;

        public MockBuilder() : this(register => {}){}

        public MockBuilder(Action<IServiceRegister> registerServices)
        {
            connectionFactory.Stub(x => x.CreateConnection()).Return(connection);
            connectionFactory.Stub(x => x.Next()).Return(false);
            connectionFactory.Stub(x => x.Succeeded).Return(true);
            connectionFactory.Stub(x => x.CurrentHost).Return(new HostConfiguration
            {
                Host = Host,
                Port = PortNumber
            });
            connectionFactory.Stub(x => x.Configuration).Return(new ConnectionConfiguration
            {
                VirtualHost = VirtualHost
            });

            connection.Stub(x => x.IsOpen).Return(true);
            
            connection.Stub(x => x.CreateModel()).WhenCalled(i =>
                {
                    var channel = MockRepository.GenerateStub<IModel>();
                    i.ReturnValue = channel;
                    channels.Add(channel);
                    channel.Stub(x => x.CreateBasicProperties()).Return(basicProperties);
                    channel.Stub(x => x.BasicConsume(null, false, null, null))
                        .IgnoreArguments()
                        .WhenCalled(consumeInvokation =>
                        {
                            var consumerTag = (string)consumeInvokation.Arguments[2];
                            var consumer = (DefaultBasicConsumer)consumeInvokation.Arguments[3];

                            consumer.HandleBasicConsumeOk(consumerTag);
                            consumers.Add(consumer);
                        }).Return("");
                });

            bus = RabbitHutch.CreateBus("host=localhost", x =>
                    {
                        registerServices(x);
                        x.Register(sp => 
                        {
                            serviceProvider = sp;
                            return connectionFactory;
                        });
                        x.Register(_ => logger);
                    });
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

        public IEasyNetQLogger Logger
        {
            get { return logger; }
        }

        public IBus Bus
        {
            get { return bus; }
        }

        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
        }
    }
}