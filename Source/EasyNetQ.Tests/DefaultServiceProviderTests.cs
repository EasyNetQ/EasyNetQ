// ReSharper disable InconsistentNaming

using EasyNetQ.DI;
using EasyNetQ.Tests.Mocking;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests
{
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

        private DefaultServiceContainer container;

        public DefaultServiceProviderTestsX()
        {
            container = new DefaultServiceContainer();

            container.Register<IRoot, Root>();
            container.Register<IChild, Child>();
            container.Register<IGrandChild, GrandChild>();
        }

        [Fact]
        public void Should_resolve_class_with_dependencies()
        {
            var service = container.Resolve<IRoot>();

            service.ShouldNotBeNull();
            service.Child.ShouldNotBeNull();
            service.Child.GrandChild.ShouldNotBeNull();
            service.Child.Second.ShouldNotBeNull();

            service.Child.GrandChild.ShouldBeTheSameAs(service.Child.Second);
        }
    }


    public class DefaultServiceProviderTests
    {
        private IServiceResolver resolver;

        private IMyFirst myFirst;
        private SomeDelegate someDelegate;

        public DefaultServiceProviderTests()
        {
            myFirst = Substitute.For<IMyFirst>();
            someDelegate = () => { };

            var defaultServiceProvider = new DefaultServiceContainer();
            
            defaultServiceProvider.Register(x => myFirst);
            defaultServiceProvider.Register(x => someDelegate);
            defaultServiceProvider.Register<IMySecond>(x => new MySecond(x.Resolve<IMyFirst>()));

            resolver = defaultServiceProvider;
        }

        [Fact]
        public void Should_resolve_a_service_interface()
        {
            var resolvedService = resolver.Resolve<IMyFirst>();
            resolvedService.ShouldBeTheSameAs(myFirst);
        }

        [Fact]
        public void Should_resolve_a_delegate_service()
        {
            var resolvedService = resolver.Resolve<SomeDelegate>();
            resolvedService.ShouldBeTheSameAs(someDelegate);
        }

        [Fact]
        public void Should_resolve_a_service_with_dependencies()
        {
            var resolvedService = resolver.Resolve<IMySecond>();
            resolvedService.First.ShouldBeTheSameAs(myFirst);
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