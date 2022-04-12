// ReSharper disable InconsistentNaming

using EasyNetQ.DI;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using System;
using System.Collections.Generic;
using Xunit;

namespace EasyNetQ.Tests.MessageVersioningTests;

public class MessageVersioningExtensionsTests
{
    [Fact]
    public void When_using_EnableMessageVersioning_extension_method_required_services_are_registered()
    {
        var serviceRegister = new ServiceRegisterStub();

        serviceRegister.EnableMessageVersioning();

        serviceRegister.AssertServiceRegistered<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>();
        serviceRegister.AssertServiceRegistered<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
    }

    private class ServiceRegisterStub : IServiceRegister
    {
        private readonly Dictionary<Type, Type> services = new();

        public void AssertServiceRegistered<TService, TImplementation>()
        {
            Assert.True(services.ContainsKey(typeof(TService)), $"No service of type {typeof(TService).Name} registered");
            Assert.Equal(typeof(TImplementation), services[typeof(TService)]); // "Implementation registered for service type {0} is not the expected type {1}", typeof(TService).Name, typeof(TImplementation).Name
        }

        public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
        {
            services.Add(serviceType, implementationType);
            return this;
        }

        public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
        {
            throw new NotImplementedException();
        }

        public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
        {
            throw new NotImplementedException();
        }

        public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            throw new NotImplementedException();
        }

        public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            throw new NotImplementedException();
        }

        public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            throw new NotImplementedException();
        }
    }
}
