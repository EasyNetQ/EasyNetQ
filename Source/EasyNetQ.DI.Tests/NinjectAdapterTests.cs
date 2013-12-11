using Castle.Windsor;
using NUnit.Framework;
using Ninject;

namespace EasyNetQ.DI.Tests
{
    [TestFixture]
    public class NinjectAdapterTests
    {
        private IKernel _container;
        private IBus _bus;

        [SetUp]
        public void SetUp()
        {
            _container = new StandardKernel();

            _container.RegisterAsEasyNetQContainerFactory();

            _bus = RabbitHutch.CreateBus("host=localhost");
        }

        [Test]
        public void Should_create_bus_with_ninject_adapter()
        {
            Assert.IsNotNull(_bus);
        }

        [TearDown]
        public void TearDown()
        {
            if (_bus != null)
            {
                _bus.Dispose();
                ((NinjectAdapter)_bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}