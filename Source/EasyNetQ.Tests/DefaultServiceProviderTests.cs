// ReSharper disable InconsistentNaming

using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultServiceProviderTestsX
    {
        private interface IRoot
        {
             IChild Child { get; }
        }

        private class Root : IRoot
        {
            public IChild Child { get; private set; }

            public Root(IChild child)
            {
                Child = child;
            }
        }

        private interface IChild
        {
            IGrandChild GrandChild { get; }
            IGrandChild Second { get; }
        }

        private class Child : IChild
        {
            public IGrandChild GrandChild { get; private set; }
            public IGrandChild Second { get; private set; }

            public Child(IGrandChild grandChild, IGrandChild second)
            {
                GrandChild = grandChild;
                Second = second;
            }
        }

        private interface IGrandChild
        {
        }

        private class GrandChild : IGrandChild
        {
        }

        private DefaultServiceProvider serviceProvider;

        [SetUp]
        public void SetUp()
        {
            serviceProvider = new DefaultServiceProvider();

            serviceProvider.Register<IRoot, Root>();
            serviceProvider.Register<IChild, Child>();
            serviceProvider.Register<IGrandChild, GrandChild>();
        }

        [Test]
        public void Should_resolve_class_with_dependencies()
        {
            var service = (IRoot)serviceProvider.Resolve(typeof (IRoot));

            service.ShouldNotBeNull();
            service.Child.ShouldNotBeNull();
            service.Child.GrandChild.ShouldNotBeNull();
            service.Child.Second.ShouldNotBeNull();

            service.Child.GrandChild.ShouldBeTheSameAs(service.Child.Second);
        }
    }


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

        [Test]
        public void Should_be_able_to_replace_bus_components()
        {
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();
            new MockBuilder(x => x.Register(_ => logger));

            logger.AssertWasCalled(x => x.DebugWrite("Trying to connect"));
        }

        [Test]
        public void Should_be_able_to_sneakily_get_the_service_provider()
        {
            IServiceProvider provider = null;
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            new MockBuilder(x => x.Register(sp =>
            {
                provider = sp;
                return logger;
            }));
            var retrievedLogger = provider.Resolve<IEasyNetQLogger>();
            retrievedLogger.DebugWrite("Hey, I'm pretending to be EasyNetQ :)");

            logger.AssertWasCalled(x => x.DebugWrite("Hey, I'm pretending to be EasyNetQ :)"));
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