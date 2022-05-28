// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.DI;
using EasyNetQ.Interception;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.Interception;

public class InterceptionExtensionsTests
{
    [Fact]
    public void When_using_EnableInterception_extension_method_required_services_are_registered()
    {
        var serviceRegister = Substitute.For<IServiceRegister>();
        serviceRegister.EnableInterception(_ => Array.Empty<IProduceConsumeInterceptor>());
        serviceRegister.Received().Register<IProduceConsumeInterceptor>(Arg.Any<Func<IServiceResolver, IProduceConsumeInterceptor>>());
    }
}
