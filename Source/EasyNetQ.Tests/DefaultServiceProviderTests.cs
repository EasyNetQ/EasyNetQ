// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Loggers;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultServiceProviderTests
    {
        private IServiceProvider serviceProvider;

        private IMyFirst myFirst;
        private SomeDelegate someDelegate;

        [SetUp]
        public void SetUp()
        {
            myFirst = MockRepository.GenerateStub<IMyFirst>();
            someDelegate = () => { };

            var defaultServiceProvider = new DefaultServiceProvider();
            
            defaultServiceProvider.Register(x => myFirst);
            defaultServiceProvider.Register(x => someDelegate);
            defaultServiceProvider.Register<IMySecond>(x => new MySecond(x.Resolve<IMyFirst>()));

            serviceProvider = defaultServiceProvider;
        }

        [Test]
        public void Should_resolve_a_service_interface()
        {
            var resolvedService = serviceProvider.Resolve<IMyFirst>();
            resolvedService.ShouldBeTheSameAs(myFirst);
        }

        [Test]
        public void Should_resolve_a_delegate_service()
        {
            var resolvedService = serviceProvider.Resolve<SomeDelegate>();
            resolvedService.ShouldBeTheSameAs(someDelegate);
        }

        [Test]
        public void Should_resolve_a_service_with_dependencies()
        {
            var resolvedService = serviceProvider.Resolve<IMySecond>();
            resolvedService.First.ShouldBeTheSameAs(myFirst);
        }

        [Test, Explicit("Requires RabbitMQ instance")]
        public void Should_be_able_to_replace_bus_components()
        {
            var logger = new TestLogger();

            using (var bus = RabbitHutch.CreateBus("host=localhost", x => x.Register<IEasyNetQLogger>(_ => logger)))
            {
                // should see the test logger on the console
            }
        }

        [Test, Explicit("Requires RabbitMQ instance")]
        public void Should_be_able_to_sneakily_get_the_service_provider()
        {
            IServiceProvider provider = null;
            using (var bus = RabbitHutch.CreateBus("host=localhost", x => x.Register<IEasyNetQLogger>(sp =>
                {
                    provider = sp;
                    return new ConsoleLogger();
                })))
            {
                // now get any services you need ...
                var logger = provider.Resolve<IEasyNetQLogger>();
                logger.DebugWrite("Hey, I'm pretending to be EasyNetQ :)");
            }
        }
    }

    public class TestLogger : IEasyNetQLogger
    {
        public void DebugWrite(string format, params object[] args)
        {
            Console.WriteLine("I am the test logger");
            Console.WriteLine(format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            Console.WriteLine("I am the test logger");
            Console.WriteLine(format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            Console.WriteLine("I am the test logger");
            Console.WriteLine(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            throw new NotImplementedException();
        }
    }

    public interface IMyFirst
    {
        
    }

    public delegate void SomeDelegate();

    public interface IMySecond
    {
        IMyFirst First { get; }
    }

    public class MySecond : IMySecond
    {
        private readonly IMyFirst myFirst;

        public MySecond(IMyFirst myFirst)
        {
            this.myFirst = myFirst;
        }


        public IMyFirst First
        {
            get { return myFirst; }
        }
    }
}

// ReSharper restore InconsistentNaming