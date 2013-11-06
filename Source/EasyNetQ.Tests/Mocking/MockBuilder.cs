﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly Stack<IModel> channelPool = new Stack<IModel>();
        readonly Dictionary<string, IBasicConsumer> consumers = new Dictionary<string, IBasicConsumer>();
        readonly IBasicProperties basicProperties = new BasicProperties();
        private readonly IEasyNetQLogger logger = MockRepository.GenerateStub<IEasyNetQLogger>();
        private readonly IBus bus;
        private IServiceProvider serviceProvider;

        public const string Host = "my_host";
        public const string VirtualHost = "my_virtual_host";
        public const int PortNumber = 1234;

        public MockBuilder() : this(register => {}){}

        public MockBuilder(Action<IServiceRegister> registerServices) : this("host=localhost", registerServices){}

        public MockBuilder(string connectionString) : this(connectionString, register => {}){}

        public MockBuilder(string connectionString, Action<IServiceRegister> registerServices)
        {
            for (int i = 0; i < 10; i++)
            {
                channelPool.Push(MockRepository.GenerateStub<IModel>());
            }

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
                VirtualHost = VirtualHost,
            });

            connection.Stub(x => x.IsOpen).Return(true);
            
            connection.Stub(x => x.CreateModel()).WhenCalled(i =>
                {
                    // Console.Out.WriteLine("\n\nMockBuilder - creating model\n{0}\n\n\n", new System.Diagnostics.StackTrace().ToString());

                    var channel = channelPool.Pop();
                    i.ReturnValue = channel;
                    channels.Add(channel);
                    channel.Stub(x => x.CreateBasicProperties()).Return(basicProperties);
                    channel.Stub(x => x.IsOpen).Return(true);
                    channel.Stub(x => x.BasicConsume(null, false, null, null))
                        .IgnoreArguments()
                        .WhenCalled(consumeInvokation =>
                        {
                            var consumerTag = (string)consumeInvokation.Arguments[2];
                            var consumer = (IBasicConsumer)consumeInvokation.Arguments[3];

                            consumer.HandleBasicConsumeOk(consumerTag);
                            consumers[consumerTag] = consumer;
                        }).Return("");
                    channel.Stub(x => x.BasicCancel(null))
                        .IgnoreArguments()
                        .WhenCalled(cancelInvokation =>
                        {
                            var consumerTag = (string)cancelInvokation.Arguments[0];
                            var consumer = consumers[consumerTag];

                            consumer.HandleBasicCancel(consumerTag);
                        });
                });

            bus = RabbitHutch.CreateBus(connectionString, x =>
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
            get { return consumers.Values.ToList(); }
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

        public IModel NextModel
        {
            get { return channelPool.Peek(); }
        }

        public IEventBus EventBus
        {
            get { return serviceProvider.Resolve<IEventBus>(); }
        }
    }
}