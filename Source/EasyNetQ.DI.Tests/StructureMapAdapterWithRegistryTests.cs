using System;
using NUnit.Framework;
using StructureMap;
using StructureMap.Configuration.DSL;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception. StructureMap has a last-to-register-wins policy 
    /// by default which has been overrided in the Adapter implementation.
    /// </summary>
    [TestFixture]
    [Explicit("Starts a connection to localhost")]
    public class StructureMapAdapterWithRegistryTests
    {
        private StructureMap.IContainer container;
        private IContainer easynetQContainer;
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            container = new Container(cfg =>
                {
                    cfg.AddRegistry<MessagingRegistry>();
                });

            container.RegisterAsEasyNetQContainerFactory();

            bus = RabbitHutch.CreateBus("host=localhost");
        
            easynetQContainer = bus.Advanced.Container;
        }

        [Test]
        public void Should_create_bus_with_structure_map_adapter()
        {
            Assert.IsNotNull(bus);
        }

        [Test]
        public void Should_construct_registered_conventions()
        {
            Assert.Greater(MyConventions.ConventionsCallCount, 0);
        }

        [Test]
        public void Should_use_registered_logger()
        {
            Assert.Greater(MyLogger.ConstructorCallCount, 0);
        }

        public class MessagingRegistry : Registry
        {
            public MessagingRegistry()
            {
                For<IEasyNetQLogger>().Singleton().Use<MyLogger>();
                For<IConventions>().Singleton().Use<MyConventions>();
            }
        }

        public class MyLogger : IEasyNetQLogger
        {
            public MyLogger()
            {
                ConstructorCallCount++;
            }

            public void DebugWrite(string format, params object[] args)
            {                
            }

            public void InfoWrite(string format, params object[] args)
            {
            }

            public void ErrorWrite(string format, params object[] args)
            {
            }

            public void ErrorWrite(Exception exception)
            {
            }

            public static int ConstructorCallCount { get; private set; }
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
            public RpcExchangeNameConvention RpcExchangeNamingConvention { get; set; }
            public RpcReturnQueueNamingConvention RpcReturnQueueNamingConvention { get; set; }
            public ConsumerTagConvention ConsumerTagConvention { get; set; }

            public static int ConventionsCallCount { get; private set; }
        }
    }
}