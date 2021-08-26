using System;
using System.Buffers;
using System.Reflection;

namespace EasyNetQ
{
    internal sealed class DefaultSerializer : ISerializer
    {
        private static readonly Type jsonBackwardCompatibilitySerializerType;
        private readonly ISerializer jsonBackwardCompatibilitySerializer;

        static DefaultSerializer()
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName("EasyNetQ.Serialization.NewtonsoftJson"));
                jsonBackwardCompatibilitySerializerType = asm.GetType("EasyNetQ.Serialization.NewtonsoftJson.JsonSerializer", throwOnError: true);
            }
            catch (Exception)
            {
                /* ignore */
            }
        }

        public DefaultSerializer()
        {
            if (jsonBackwardCompatibilitySerializerType == null)
                throw new InvalidOperationException(@"DefaultSerializer can not find EasyNetQ.Serialization.NewtonsoftJson.dll which is used by default to serialize messages (for backward compatibility).
To solve this issue:
1. Use EasyNetQ.All package, not EasyNetQ. EasyNetQ.All package contains both EasyNetQ and EasyNetQ.Serialization.NewtonsoftJson.
   or
2. Specify serializer explicitly via one of RegisterEasyNetQ/RegisterBus/CreateBus overloads which take IServiceRegister parameter, for example:
   your_di_container.RegisterEasyNetQ(""host = localhost"", r => r.Register<ISerializer, JsonSerializer>());
   RabbitHutch.CreateBus(""host = localhost"", r => r.Register<ISerializer, MySerializer>());
");

            jsonBackwardCompatibilitySerializer = (ISerializer)Activator.CreateInstance(jsonBackwardCompatibilitySerializerType);
        }

        public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
        {
            return jsonBackwardCompatibilitySerializer.MessageToBytes(messageType, message);
        }

        public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
        {
            return jsonBackwardCompatibilitySerializer.BytesToMessage(messageType, bytes);
        }
    }
}
