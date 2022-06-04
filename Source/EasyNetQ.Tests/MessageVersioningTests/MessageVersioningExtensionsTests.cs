// ReSharper disable InconsistentNaming

using EasyNetQ.DI;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using System;
using System.Collections.Generic;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.MessageVersioningTests;

public class MessageVersioningExtensionsTests
{
    [Fact]
    public void When_using_EnableMessageVersioning_extension_method_required_services_are_registered()
    {
        var serviceRegister = Substitute.For<IServiceRegister>();

        serviceRegister.EnableMessageVersioning();

        serviceRegister.Received()
            .Register(typeof(IExchangeDeclareStrategy), typeof(VersionedExchangeDeclareStrategy));
        serviceRegister.Received()
            .Register(typeof(IMessageSerializationStrategy), typeof(VersionedMessageSerializationStrategy));
    }

}
