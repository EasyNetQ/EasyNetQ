// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using EasyNetQ.Producer;
using NUnit.Framework;
using EasyNetQ.MessageVersioning;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    [TestFixture]
    public class MessageVersioningExtensionsTests
    {
        [Test]
        public void When_using_EnableMessageVersioning_extension_method_required_services_are_registered()
        {
            var serviceRegister = new ServiceRegisterStub();

            serviceRegister.EnableMessageVersioning();

            serviceRegister.AssertServiceRegistered<IPublishExchangeDeclareStrategy, VersionedPublishExchangeDeclareStrategy>();
            serviceRegister.AssertServiceRegistered<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
        }

        private class ServiceRegisterStub : IServiceRegister
        {
            private readonly Dictionary<Type, Type> _services = new Dictionary<Type, Type>();

            public IServiceRegister Register<TService>( Func<IServiceProvider, TService> serviceCreator ) where TService : class
            {
                throw new NotImplementedException();
            }

            public IServiceRegister Register<TService, TImplementation>() where TService : class where TImplementation : class, TService
            {
                _services.Add( typeof( TService ), typeof( TImplementation ) );
                return this;
            }

            public void AssertServiceRegistered<TService, TImplementation>()
            {
                Assert.That( _services.ContainsKey( typeof(TService)), "No service of type {0} registered", typeof(TService).Name );
                Assert.That( _services[ typeof( TService ) ], Is.EqualTo(typeof(TImplementation)), "Implementation registered for service type {0} is not the expected type {1}", typeof( TService ).Name, typeof( TImplementation ).Name );
            }
        }
    }
}