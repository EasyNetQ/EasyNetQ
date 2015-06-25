// ReSharper disable InconsistentNaming

using System;
using System.IO;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class RabbitHutchTests
    {
        [Test]
        public void Should_be_able_to_replace_default_service_provider()
        {
            var bus = MockRepository.GenerateStub<IBus>();
            var container = new MyAlternativeContainer(bus);

            try
            {
                RabbitHutch.SetContainerFactory(() => container);

                var resolvedBus = RabbitHutch.CreateBus("host=localhost");

                resolvedBus.ShouldBeTheSameAs(bus);

//                Console.Out.WriteLine(container.RegisteredComponents);
            }
            finally
            {
                RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
            }
        }
    }

    public class MyAlternativeContainer : IContainer
    {
        private readonly IBus bus;
        private readonly StringWriter writer = new StringWriter();

        public string RegisteredComponents
        {
            get { return writer.GetStringBuilder().ToString(); }
        }

        public MyAlternativeContainer(IBus bus)
        {
            this.bus = bus;
        }

        public TService Resolve<TService>() where TService : class
        {
            var theBus = bus as TService;
            if (theBus == null)
            {
                throw new Exception("Only expected a single resolve call for IBus");
            }
            return theBus;
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) 
            where TService : class
        {
            writer.WriteLine("{0} => factory", typeof(TService).Name);
            writer.Flush();

            return this;
        }

        public IServiceRegister Register<TService, TImplementation>() 
            where TService : class 
            where TImplementation : class, TService
        {
            writer.WriteLine("{0} => {1}", typeof(TService).Name, typeof(TImplementation).Name);
            writer.Flush();

            return this;
        }
    }
}

// ReSharper restore InconsistentNaming