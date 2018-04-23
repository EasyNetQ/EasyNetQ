using System;
using Autofac.Features.ResolveAnything;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    public abstract class ContainerAdapterTests
    {
        protected abstract IServiceRegister CreateServiceRegister();
        protected abstract IServiceResolver CreateServiceResolver();

        IServiceRegister register;
        IServiceResolver resolver;
        private ILastRegistrationWins last;

        protected ContainerAdapterTests()
        {
            Initialize();
        }

        void Initialize()
        {
            this.register = CreateServiceRegister();

            last = new LastRegistrationWins();

            this.register.Register(last);
            this.register.Register(new LastRegistrationWins());
            this.register.Register<ISingleton, Singleton>();
            this.register.Register<ITransient, Transient>(Lifetime.Transient);

            this.resolver = CreateServiceResolver();
        }


        [Fact]
        public void Should_last_registration_win()
        {
            Assert.Equal(last, resolver.Resolve<ILastRegistrationWins>());
        }

        [Fact]
        public void Should_singleton_created_once()
        {
            Assert.Same(resolver.Resolve<ISingleton>(), resolver.Resolve<ISingleton>());
        }

        [Fact]
        public void Should_transient_created_every_time()
        {
            Assert.NotSame(resolver.Resolve<ITransient>(), resolver.Resolve<ITransient>());
        }

        [Fact]
        public void Should_resolve_service_resolver()
        {
            Assert.Same(resolver, resolver.Resolve<IServiceResolver>());
        }
        
        [Fact]
        public void Should_resolve_service_register()
        {
            Assert.Same(register, resolver.Resolve<IServiceRegister>());
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