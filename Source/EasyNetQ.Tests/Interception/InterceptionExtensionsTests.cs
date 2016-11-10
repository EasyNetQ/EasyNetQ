// ReSharper disable InconsistentNaming

using System;
using NUnit.Framework;
using EasyNetQ.Interception;
using NSubstitute;

namespace EasyNetQ.Tests.Interception
{
    [TestFixture]
    public partial class InterceptionExtensionsTests
    {
        

        [Test]
        public void When_using_EnableInterception_extension_method_required_services_are_registered()
        {
            var serviceRegister = Substitute.For<IServiceRegister>();
            serviceRegister.EnableInterception(x => { });
            serviceRegister.Received().Register(Arg.Any<Func<IServiceProvider, IProduceConsumeInterceptor>>());
        }

        [Test]
        public void When_using_EnableGZipCompression_extension_method_required_interceptor_is_added()
        {
            var interceptorRegistrator = Substitute.For<IInterceptorRegistrator>();
            interceptorRegistrator.EnableGZipCompression();
            interceptorRegistrator.Received().Add(Arg.Any<GZipInterceptor>());
        }


        [Test]
        public void When_using_EnableTripleDESEncryption_extension_method_required_interceptor_is_added()
        {
            var interceptorRegistrator = Substitute.For<IInterceptorRegistrator>();
            interceptorRegistrator.EnableTripleDESEncryption(new byte[0], new byte[0]);
            interceptorRegistrator.Received().Add(Arg.Any<TripleDESInterceptor>());
        }
    }
}