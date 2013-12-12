using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Ninject;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception, and StructureMap has a last-to-register-wins policy.
    /// </summary>
    [TestFixture]
    [Explicit("Ninject doesn't allow multiple registrations with get first registered semantics.")]
    public class NinjectAdapterTests
    {
        private IKernel container;
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            container = new StandardKernel();

            container.RegisterAsEasyNetQContainerFactory();

            bus = new MockBuilder().Bus;
        }

        [Test]
        public void Should_create_bus_with_ninject_adapter()
        {
            // TODO: Ninject doesn't allow multiple registrations with
            // get first registered semantics.
//            Assert.IsNotNull(bus);
        }

        [TearDown]
        public void TearDown()
        {
            if (bus != null)
            {
                bus.Dispose();
                ((NinjectAdapter)bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }
    }
}