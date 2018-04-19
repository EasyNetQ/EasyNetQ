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


    public class DefaultServiceContainerTests
    {
        private IServiceResolver resolver;

        private IFirst first;
        private SomeDelegate someDelegate;
        private IThird third;

        public DefaultServiceContainerTests()
        {
            first = Substitute.For<IFirst>();
            someDelegate = () => { };
            third = Substitute.For<IThird>();

            var defaultServiceProvider = new DefaultServiceContainer();

            defaultServiceProvider.Register(first);
            defaultServiceProvider.Register(someDelegate);
            defaultServiceProvider.Register<ISecond, Second>();
            defaultServiceProvider.Register(Substitute.For<IThird>());
            defaultServiceProvider.Register(third);
          
            resolver = defaultServiceProvider;
        }

        [Fact]
        public void Should_resolve_a_service_interface()
        {
            var resolvedService = resolver.Resolve<IFirst>();
            resolvedService.ShouldBeTheSameAs(first);
        }

        [Fact]
        public void Should_resolve_a_delegate_service()
        {
            var resolvedService  = resolver.Resolve<SomeDelegate>();
            resolvedService.ShouldBeTheSameAs(someDelegate);
        }

        [Fact]
        public void Should_resolve_a_service_with_dependencies()
        {
            var resolvedService = resolver.Resolve<ISecond>();
            resolvedService.First.ShouldBeTheSameAs(first);
        }

        [Fact]
        public void Should_resolve_first_registered_implementation()
        {
            var resolvedService = resolver.Resolve<IThird>();
            resolvedService.ShouldBeTheSameAs(third);
        }
    }

    public interface IFirst
    {
    }

    public delegate void SomeDelegate();

    public interface ISecond
    {
        IFirst First { get; }
    }

    public interface IThird
    {
    }

    public class Second : ISecond
    {
        public Second(IFirst first)
        {
            First = first;
        }

        public IFirst First { get; }
    }


}

// ReSharper restore InconsistentNaming