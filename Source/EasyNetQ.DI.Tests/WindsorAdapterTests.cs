using Castle.Windsor;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception, and StructureMap has a last-to-register-wins policy.
    /// </summary>
    [TestFixture]
    public class WindsorAdapterTests
    {
        private IWindsorContainer container;
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            container = new WindsorContainer();

            container.RegisterAsEasyNetQContainerFactory();

            bus = new MockBuilder().Bus;
        }

        [Test]
        public void Should_create_bus_with_windsor_adapter()
        {
            Assert.IsNotNull(bus);
        }

        [TearDown]
        public void TearDown()
        {
            if (bus != null)
            {
                bus.Dispose();
                ((WindsorAdapter)bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}