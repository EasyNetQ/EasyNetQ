using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace EasyNetQ
{
    /// <inheritdoc />
    public class LegacyTypeNameSerializer : ITypeNameSerializer
    {
        private readonly ConcurrentDictionary<string, Type> deserializedTypes = new ConcurrentDictionary<string, Type>();

        /// <inheritdoc />
        public Type DeSerialize(string typeName)
        {
            Preconditions.CheckNotBlank(typeName, "typeName");

            return deserializedTypes.GetOrAdd(typeName, t =>
            {
                var nameParts = t.Split(':');
                if (nameParts.Length != 2)
                {
                    throw new EasyNetQException("type name {0}, is not a valid EasyNetQ type name. Expected Type:Assembly", t);
                }
                var type = Type.GetType(nameParts[0] + ", " + nameParts[1]);
                if (type == null)
                {
                    type = Type.GetType(nameParts[0]);
                }
                if (type == null)
                {
                    throw new EasyNetQException("Cannot find type {0}", t);
                }
                return type;
            });
        }

        private readonly ConcurrentDictionary<Type, string> serializedTypes = new ConcurrentDictionary<Type, string>();

        /// <inheritdoc />
        public string Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");

            return serializedTypes.GetOrAdd(type, t =>
            {
                var typeName = t.FullName + ":" + t.GetTypeInfo().Assembly.GetName().Name;
                if (typeName.Length > 255)
                {
                    throw new EasyNetQException("The serialized name of type '{0}' exceeds the AMQP " +
                                                "maximum short string length of 255 characters.", t.Name);
                }
                return typeName;
            });
        }
    }
}
