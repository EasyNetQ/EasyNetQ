using System;
using EasyNetQ.Tests.Mocking;
using SimpleInjector;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception, and StructureMap has a last-to-register-wins policy.
    /// </summary>    
    public class SimpleInjectorAdapterTests : IDisposable
    {
        private Container container;
        private IBus bus;

        public SimpleInjectorAdapterTests()
        {
            container = new Container();

            container.RegisterAsEasyNetQContainerFactory();

            bus = new MockBuilder(register =>
                register.Register<IConventions>(r => new TestConventions(new TypeNameSerializer())
            )).Bus;
        }

        [Fact]
        public void Should_create_bus_with_ninject_adapter()
        {
            Assert.NotNull(bus);
        }

        [Fact]
        public void Should_resolve_test_conventions()
        {
            Assert.NotNull(bus);

            var rabbitBus = (RabbitBus)bus;

            Assert.True(rabbitBus.Advanced.Conventions is TestConventions);
        }

        public void Dispose()
        {
            if (bus != null)
            {
                bus.Dispose();
                ((SimpleInjectorAdapter)bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}
