using System;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    public abstract class ContainerAdapterTests<TState> : IDisposable
    {
        private readonly IServiceRegister register;
        private readonly IServiceResolver resolver;
        private readonly ILastRegistrationWins last;        
        
        protected ContainerAdapterTests(TState state, Func<TState, IServiceRegister> registerFactory, Func<TState, IServiceResolver> resolverFactory)
        {
            register = registerFactory.Invoke(state);
            
            last = new LastRegistrationWins();

            register.Register(last);
            register.Register(new LastRegistrationWins());
            register.Register<ISingleton, Singleton>();
            register.Register<ITransient, Transient>(Lifetime.Transient);

            resolver = resolverFactory.Invoke(state);
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

        public void Dispose()
        {
            resolver.Dispose();
        }
    }
}