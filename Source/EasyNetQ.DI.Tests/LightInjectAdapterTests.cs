using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using LightInject;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject & LightInject don't allow more than one registration of a
    /// service, they throw an exception, and StructureMap has a last-to-register-wins
    /// policy.
    /// </summary>    
    [TestFixture]    
    public class LightInjectAdapterTests
    {
        private IServiceContainer container;
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            container = new ServiceContainer();

            container.RegisterAsEasyNetQContainerFactory();

            bus = new MockBuilder(register =>
                register.Register<IConventions>(r => new TestConventions(new TypeNameSerializer())
            )).Bus;
        }

        [Test]
        public void Should_create_bus_with_lightinject_adapter()
        {
            Assert.IsNotNull(bus);
        }

        [Test]
        public void Should_resolve_test_conventions()
        {
            Assert.IsNotNull(bus);

            var rabbitBus = (RabbitBus)bus;

            Assert.IsTrue(rabbitBus.Advanced.Conventions is TestConventions);
        }

        [TearDown]
        public void TearDown()
        {
            if (bus != null)
            {
                bus.Dispose();
                ((LightInjectAdapter)bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}
