using System;
using System.Collections.Generic;
using EasyNetQ.DI;
using Xunit;

namespace EasyNetQ.Serialization.Tests
{
    public class ServiceRegisterExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetSerializerRegisterActions))]
        public void Should_register_serializer(Action<IServiceRegister> register)
        {
            var serviceRegister = new ServiceRegisterStub();
            serviceRegister.EnableNewtonsoftJson();
            serviceRegister.AssertServiceRegistered<ISerializer>();
        }

        public static IEnumerable<object[]> GetSerializerRegisterActions()
        {
            yield return new object[] { GetRegisterAction(x => x.EnableNewtonsoftJson()) };
        }

        private static Action<IServiceRegister> GetRegisterAction(Action<IServiceRegister> action) => action;

        private class ServiceRegisterStub : IServiceRegister
        {
            private readonly HashSet<Type> services = new();

            public void AssertServiceRegistered<TService>()
            {
                Assert.True(services.Contains(typeof(TService)), $"No service of type {typeof(TService).Name} registered");
            }

            public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
            {
                services.Add(typeof(TService));
                return this;
            }

            public IServiceRegister Register<TService>(TService instance) where TService : class
            {
                services.Add(typeof(TService));
                return this;
            }

            public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
            {
                services.Add(typeof(TService));
                return this;
            }

            public IServiceRegister Register(Type serviceType, Type implementingType, Lifetime lifetime = Lifetime.Singleton)
            {
                throw new NotImplementedException();
            }
        }
    }
}
