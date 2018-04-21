using System;
using NSubstitute;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    public class ContainerAdapterTest<TContainer> where TContainer : IServiceRegister, IServiceResolver
    {
        private readonly TContainer container;
        private readonly ILastRegistrationWins last;

        public ContainerAdapterTest(Func<TContainer> factory)
        {
            container = factory.Invoke();

            last = Substitute.For<ILastRegistrationWins>();

            container.Register(Substitute.For<ILastRegistrationWins>());
            container.Register(last);

            container.Register<ISingleton, Singleton>();
            container.Register<ITransient, Transient>(Lifetime.Transient);
        }

        [Fact]
        public void Should_last_registration_win()
        {
            Assert.Equal(last, container.Resolve<ILastRegistrationWins>());
        }

        [Fact]
        public void Should_singleton_created_once()
        {
            Assert.Same(container.Resolve<ISingleton>(), container.Resolve<ISingleton>());
        }

        [Fact]
        public void Should_transient_created_every_time()
        {
            Assert.NotSame(container.Resolve<ISingleton>(), container.Resolve<ISingleton>());
        }

        [Fact]
        public void Should_resolve_service_resolver()
        {
            Assert.Same(container, container.Resolve<IServiceResolver>());
        }
        
        [Fact]
        public void Should_resolve_service_register()
        {
            Assert.Same(container, container.Resolve<IServiceRegister>());
        }

        private interface ILastRegistrationWins
        {
        }
        
        private interface ISingleton
        {
        }

        private class Singleton : ISingleton
        {
        }
        
        private interface ITransient
        {
        }

        private class Transient : ITransient
        {
        }
    }
}