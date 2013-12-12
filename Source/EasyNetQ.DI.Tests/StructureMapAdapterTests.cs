using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using StructureMap;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception, and StructureMap has a last-to-register-wins policy.
    /// </summary>
    [TestFixture]
    [Explicit("Starts a connection to localhost")]
    public class StructureMapAdapterTests
    {
        private StructureMap.IContainer container;
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            container = new Container();

            container.RegisterAsEasyNetQContainerFactory();

            bus = new MockBuilder().Bus;
        }

        [Test]
        public void Should_create_bus_with_structure_map_adapter()
        {
            Assert.IsNotNull(bus);
        }

        [TearDown]
        public void TearDown()
        {
            if (bus != null)
            {
                bus.Dispose();
                ((StructureMapAdapter)bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}
