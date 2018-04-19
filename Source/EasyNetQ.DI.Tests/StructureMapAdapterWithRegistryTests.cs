﻿using System;
using EasyNetQ.Tests;
using StructureMap;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception. StructureMap has a last-to-register-wins policy 
    /// by default which has been overrided in the Adapter implementation.
    /// </summary>
    [Explicit("Starts a connection to localhost")]
    public class StructureMapAdapterWithRegistryTests : IDisposable
    {
        private StructureMap.IContainer container;
        private IContainer easynetQContainer;
        private IBus bus;

        public StructureMapAdapterWithRegistryTests()
        {
            container = new Container(cfg =>
                {
                    cfg.AddRegistry<MessagingRegistry>();
                });

            container.RegisterAsEasyNetQContainerFactory();

            bus = RabbitHutch.CreateBus("host=localhost");
        
            easynetQContainer = bus.Advanced.Container;
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_create_bus_with_structure_map_adapter()
        {
            Assert.NotNull(bus);
        }

        [Fact]
        public void Should_construct_registered_conventions()
        {
            Assert.True(MyConventions.ConventionsCallCount > 0);
        }

        public class MessagingRegistry : Registry
        {
            public MessagingRegistry()
            {
                For<IConventions>().Singleton().Use<MyConventions>();
            }
        }

        public class MyConventions : IConventions
        {
            private readonly ITypeNameSerializer _typeNameSerializer;

            public MyConventions(ITypeNameSerializer typeNameSerializer)
            {
                _typeNameSerializer = typeNameSerializer;
                ExchangeNamingConvention = type => { return string.Format("exchange{0}_", type.Name.Replace(".", "_")); };
                ConventionsCallCount++;
            }

            public ExchangeNameConvention ExchangeNamingConvention { get; set; }
            public TopicNameConvention TopicNamingConvention { get; set; }
            public QueueNameConvention QueueNamingConvention { get; set; }
            public RpcRoutingKeyNamingConvention RpcRoutingKeyNamingConvention { get; set; }
            public ErrorQueueNameConvention ErrorQueueNamingConvention { get; set; }
            public ErrorExchangeNameConvention ErrorExchangeNamingConvention { get; set; }
            public RpcExchangeNameConvention RpcRequestExchangeNamingConvention { get; set; }
            public RpcExchangeNameConvention RpcResponseExchangeNamingConvention { get; set; }
            public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }
            public ConsumerTagConvention ConsumerTagConvention { get; set; }

            public static int ConventionsCallCount { get; private set; }
        }
    }
}