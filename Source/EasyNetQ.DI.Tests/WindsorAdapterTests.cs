using Castle.Windsor;
using NUnit.Framework;

namespace EasyNetQ.DI.Tests
{
    [TestFixture]
    public class WindsorAdapterTests
    {
        private IWindsorContainer _container;
        private IBus _bus;

        [SetUp]
        public void SetUp()
        {
            _container = new WindsorContainer();

            _container.RegisterAsEasyNetQContainerFactory();

            _bus = RabbitHutch.CreateBus("host=localhost");
        }

        [Test]
        public void Should_create_bus_with_windsor_adapter()
        {
            Assert.IsNotNull(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            if (_bus != null)
            {
                _bus.Dispose();
                ((WindsorAdapter)_bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}