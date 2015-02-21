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
            builder.RegisterType<TestConventions>().As<IConventions>();
            
            container = builder.RegisterAsEasyNetQContainerFactory(() => new MockBuilder().Bus);
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

        [Test]
        public void Should_resolve_autosubscriber()
        {
            Assert.IsNotNull(bus);

            Assert.IsTrue(bus is RabbitBus);

            var rabbitBus = (RabbitBus)bus;

            Assert.IsTrue(rabbitBus.Advanced.Conventions is TestConventions);
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