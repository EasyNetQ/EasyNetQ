using Autofac;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    ///     EasyNetQ expects that the DI container will follow a first-to-register-wins
    ///     policy. The internal DefaultServiceProvider works this way, as does Windsor.
    ///     However, Ninject doesn't allow more than one registration of a service, it
    ///     throws an exception, and StructureMap and Autofac have a last-to-register-wins policy.
    /// </summary>
    [TestFixture]
    [Explicit("Starts a connection to localhost")]
    public class AutofacAdapterTests
    {
        [SetUp]
        public void SetUp()
        {
            builder = new ContainerBuilder();
            builder.Register(c => new MockBuilder().Bus).As<IBus>();
            container = builder.RegisterAsEasyNetQContainerFactory();
            bus = container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            container.Dispose();
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }

        private ContainerBuilder builder;
        private IBus bus;
        private Autofac.IContainer container;

        [Test]
        public void Should_create_bus_with_autofac_module()
        {
            Assert.IsNotNull(bus);
        }
    }
}