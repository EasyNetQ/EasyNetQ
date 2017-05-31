using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace EasyNetQ
{
    public class DynamicTypeNameSerializer : ITypeNameSerializer
    {
        private readonly ConcurrentDictionary<string, Type> deserializedTypes = new ConcurrentDictionary<string, Type>();
        private readonly ConcurrentDictionary<string, Assembly> assemblies = new ConcurrentDictionary<string, Assembly>();

        public Type DeSerialize(string typeName)
        {
            Preconditions.CheckNotBlank(typeName, nameof(typeName));
            return deserializedTypes.GetOrAdd(typeName, GetType);
        }

        private Type GetType(string t)
        {
            var nameParts = t.Split(':');
            if (nameParts.Length != 2)
            {
                throw new EasyNetQException("type name {0}, is not a valid EasyNetQ type name. Expected Type:Assembly", t);
            }

            var assemblyName = nameParts[1];
            var typeName = nameParts[0];

            var type = Type.GetType(typeName + ", " + assemblyName);

            if (type == null)
            {
                var assembly = assemblies.GetOrAdd(assemblyName, a =>
                    AppDomain.CurrentDomain.GetAssemblies().Single(aa => GetAssemblyName(aa).Equals(a, StringComparison.Ordinal)));
                type = assembly.GetType(typeName);
            }

            if (type == null)
            {
                throw new EasyNetQException("Cannot find type {0}", t);
            }
            return type;
        }

        private readonly ConcurrentDictionary<Type, string> serializedTypes = new ConcurrentDictionary<Type, string>();

        public string Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");

            return serializedTypes.GetOrAdd(type, t =>
            {
                var typeName = t.FullName + ":" + GetAssemblyName(t.Assembly);
                if (typeName.Length > 255)
                {
                    throw new EasyNetQException("The serialized name of type '{0}' exceeds the AMQP " +
                                                "maximum short string length of 255 characters.", t.Name);
                }
                return typeName;
            });
        }

        private static string GetAssemblyName(Assembly assembly) => assembly.GetName().Name;
    }
}
