using System;
using System.Collections.Generic;
using EasyNetQ.DI;
using Xunit;

namespace EasyNetQ.Serialization.Tests;

public class ServiceRegisterExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetSerializerRegisterActions))]
    public void Should_register_serializer(Action<IServiceRegister> register)
    {
        var serviceRegister = new ServiceRegisterStub();
        register(serviceRegister);
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

        public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
        {
            services.Add(serviceType);
            return this;
        }

        public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
        {
            services.Add(serviceType);
            return this;
        }

        public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
        {
            services.Add(serviceType);
            return this;
        }

        public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            services.Add(serviceType);
            return this;
        }

        public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            services.Add(serviceType);
            return this;
        }

        public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            services.Add(serviceType);
            return this;
        }
    }
}
