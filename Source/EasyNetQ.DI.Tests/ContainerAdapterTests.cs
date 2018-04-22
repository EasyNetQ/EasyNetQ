using System;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    public abstract class ContainerAdapterTests<TContainer> where TContainer : class, IServiceRegister, IServiceResolver
    {
        private readonly TContainer container;
        private readonly ILastRegistrationWins last;

        protected ContainerAdapterTests(Func<TContainer> factory)
        {
            container = factory.Invoke();

            last = new LastRegistrationWins();

            container.Register(new LastRegistrationWins());
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
            Assert.NotSame(container.Resolve<ITransient>(), container.Resolve<ITransient>());
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

        public interface ILastRegistrationWins
        {
        }

        public class LastRegistrationWins : ILastRegistrationWins
        {
        }

        public interface ISingleton
        {
        }

        public class Singleton : ISingleton
        {
        }

        public interface ITransient
        {
        }

        public class Transient : ITransient
        {
        }
    }
}