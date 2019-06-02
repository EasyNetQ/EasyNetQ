// ReSharper disable InconsistentNaming

using EasyNetQ.DI;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using System;
using System.Collections.Generic;
using Xunit;

namespace EasyNetQ.Tests.MessageVersioningTests
{
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
            private readonly Dictionary<Type, Type> services = new Dictionary<Type, Type>();

            public void AssertServiceRegistered<TService, TImplementation>()
            {
                Assert.True(services.ContainsKey(typeof(TService)), $"No service of type {typeof(TService).Name} registered");
                Assert.Equal(typeof(TImplementation), services[typeof(TService)]); // "Implementation registered for service type {0} is not the expected type {1}", typeof(TService).Name, typeof(TImplementation).Name
            }

            public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
            {
                services.Add(typeof(TService), typeof(TImplementation));
                return this;
            }

            public IServiceRegister Register<TService>(TService instance) where TService : class
            {
                throw new NotImplementedException();
            }

            public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
            {
                throw new NotImplementedException();
            }
        }
    }
}
