using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Mocking
{
    public class MockBuilder
    {
        readonly IConnectionFactory connectionFactory = MockRepository.GenerateStub<IConnectionFactory>();
        readonly IConnection connection = MockRepository.GenerateStub<IConnection>();
        readonly IModel channel = MockRepository.GenerateStub<IModel>();
        readonly IBasicProperties basicProperties = new BasicProperties();
        private readonly IBus bus;

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
            
            connection.Stub(x => x.CreateModel()).Return(channel);

            channel.Stub(x => x.CreateBasicProperties()).Return(basicProperties);

            bus = RabbitHutch.CreateBus("host=localhost", x =>
                    {
                        registerServices(x);
                        x.Register(_ => connectionFactory);
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

        public IModel Channel
        {
            get { return channel; }
        }

        public IBasicProperties BasicProperties
        {
            get { return basicProperties; }
        }

        public IBus Bus
        {
            get { return bus; }
        }
    }
}