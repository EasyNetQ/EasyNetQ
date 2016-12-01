using System;
using Autofac;
using EasyNetQ.Tests;
using EasyNetQ.Tests.Mocking;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    ///     EasyNetQ expects that the DI container will follow a first-to-register-wins
    ///     policy. The internal DefaultServiceProvider works this way, as does Windsor.
    ///     However, Ninject doesn't allow more than one registration of a service, it
    ///     throws an exception, and StructureMap and Autofac have a last-to-register-wins policy.
    /// </summary>
    [Explicit("Starts a connection to localhost")]
    public class AutofacAdapterTests : IDisposable
    {
        public AutofacAdapterTests()
        {
            builder = new ContainerBuilder();
            builder.RegisterType<TestConventions>().As<IConventions>();
            
            container = builder.RegisterAsEasyNetQContainerFactory(() => new MockBuilder().Bus);
            bus = container.Resolve<IBus>();
        }

        public void Dispose()
        {
            container?.Dispose();
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }

        private ContainerBuilder builder;
        private IBus bus;
        private Autofac.IContainer container;

        [Fact]
        public void Should_create_bus_with_autofac_module()
        {
            Assert.NotNull(bus);
        }

        [Fact]
        public void Should_resolve_autosubscriber()
        {
            Assert.NotNull(bus);

            Assert.True(bus is RabbitBus);

            var rabbitBus = (RabbitBus)bus;

            Assert.True(rabbitBus.Advanced.Conventions is TestConventions);
        }
    }

    public class TestConventions : Conventions
    {
        public TestConventions(ITypeNameSerializer typeNameSerializer) : base(typeNameSerializer)
        {
            QueueNamingConvention = (messageType, subscriptionId) =>
            {
                var typeName = typeNameSerializer.Serialize(messageType);
                return string.Format("{0}_{1}", typeName, subscriptionId);
            };

        }
    }
}