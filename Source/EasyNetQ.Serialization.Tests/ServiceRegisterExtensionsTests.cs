using System;
using System.Collections.Generic;
using EasyNetQ.DI;
using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Serialization.Tests;

public class ServiceRegisterExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetSerializerRegisterActions))]
    public void Should_register_serializer(Action<IServiceRegister> register, Type serializerType)
    {
        var serviceRegister = Substitute.For<IServiceRegister>();
        register(serviceRegister);
        serviceRegister.Received().Register(typeof(ISerializer), serializerType);
    }

    public static IEnumerable<object[]> GetSerializerRegisterActions()
    {
        yield return new object[] { GetRegisterAction(x => x.EnableNewtonsoftJson()), typeof(NewtonsoftJsonSerializer) };
        yield return new object[] { GetRegisterAction(x => x.EnableSystemTextJson()), typeof(SystemTextJsonSerializer) };
    }

    private static Action<IServiceRegister> GetRegisterAction(Action<IServiceRegister> action) => action;
}
