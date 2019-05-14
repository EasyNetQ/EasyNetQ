using System;
using System.Reflection;

namespace EasyNetQ
{
    internal sealed class DefaultSerializer : ISerializer
    {
        private static readonly Type _jsonBackwardCompatibilitySerializerType;
        private readonly ISerializer _jsonBackwardCompatibilitySerializer;

        static DefaultSerializer()
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName("EasyNetQ.Serialization.NewtonsoftJson"));
                _jsonBackwardCompatibilitySerializerType = asm.GetType("EasyNetQ.Serialization.NewtonsoftJson.JsonSerializer", throwOnError: true);
            }
            catch (Exception)
            {
                /* ignore */
            }
        }

        public DefaultSerializer()
        {
            if (_jsonBackwardCompatibilitySerializerType == null)
                throw new InvalidOperationException(@"DefaultSerializer can not find EasyNetQ.Serialization.NewtonsoftJson.dll which is used by default to serialize messages (for backward compatibility).
To solve this issue:
1. Use EasyNetQ package, not EasyNetQ.Core. EasyNetQ package contains both EasyNetQ.Core and EasyNetQ.Serialization.NewtonsoftJson.
   or
2. Specify serializer explicitly via one of RegisterEasyNetQ/RegisterBus/CreateBus overloads which take IServiceRegister parameter, for example:
   your_di_container.RegisterEasyNetQ(""host = localhost"", r => r.Register<ISerializer, JsonSerializer>());
   RabbitHutch.CreateBus(""host = localhost"", r => r.Register<ISerializer, MySerializer>());
");

            _jsonBackwardCompatibilitySerializer = (ISerializer)Activator.CreateInstance(_jsonBackwardCompatibilitySerializerType);
        }

        public byte[] MessageToBytes(Type messageType, object message)
        {
            return _jsonBackwardCompatibilitySerializer.MessageToBytes(messageType, message);
        }

        public object BytesToMessage(Type messageType, byte[] bytes)
        {
            return _jsonBackwardCompatibilitySerializer.BytesToMessage(messageType, bytes);
        }
    }
}
