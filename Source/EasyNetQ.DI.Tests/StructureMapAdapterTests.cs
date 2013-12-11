using NUnit.Framework;
using StructureMap;

namespace EasyNetQ.DI.Tests
{
    [TestFixture]
    public class StructureMapAdapterTests
    {
        private StructureMap.IContainer _container;
        private IBus _bus;

        [SetUp]
        public void SetUp()
        {
            _container = new Container();

            _container.RegisterAsEasyNetQContainerFactory();

            _bus = RabbitHutch.CreateBus("host=localhost");
        }

        [Test]
        public void Should_create_bus_with_structure_map_adapter()
        {
            Assert.IsNotNull(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            if (_bus != null)
            {
                _bus.Dispose();
                ((StructureMapAdapter)_bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}
