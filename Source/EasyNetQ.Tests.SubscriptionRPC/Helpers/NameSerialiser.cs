using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.Tests.SubscriptionRPC.Helpers {
    public class NameSerialiser : ITypeNameSerializer 
    {
        private readonly ConcurrentDictionary<string, Type> deserializedTypes = new ConcurrentDictionary<string, Type>();

        public Type DeSerialize(string typeName) {
            //Preconditions.CheckNotBlank(typeName, "typeName");

            return deserializedTypes.GetOrAdd(typeName, t => {
                var nameParts = t.Split(':');
                //if (nameParts.Length != 2) {
                //    throw new EasyNetQException("type name {0}, is not a valid EasyNetQ type name. Expected Type:Assembly", t);
                //}
                var type = Type.GetType(nameParts.ToSingleString(",").TrimEnd(','));
                if (type == null) {
                    throw new EasyNetQException("Cannot find type {0}", t);
                }
                return type;
            });
        }

        private readonly ConcurrentDictionary<Type, string> serializedTypes = new ConcurrentDictionary<Type, string>();

        public string Serialize(Type type) {
            if (type.IsGenericType) {
                var genericType = type.GetGenericTypeDefinition();
                var genericTypeAssemblyName = genericType.Assembly.GetName().Name;
                var typeNameBuilder = new StringBuilder();
                typeNameBuilder.Append(genericType.FullName);
                typeNameBuilder.Append("[[");
                var first = true;
                foreach (var genericArgumentType in type.GetGenericArguments()) {
                    if (!first)
                        typeNameBuilder.Append(":");
                    typeNameBuilder.Append(Serialize(genericArgumentType));
                    first = false;
                }
                typeNameBuilder.Append("]]");
                typeNameBuilder.Append(":");
                typeNameBuilder.Append(genericTypeAssemblyName);
                var typeName = typeNameBuilder.ToString();
                if (typeName.Length > 255) {
                    throw new Exception("The serialized name of type exceeds the AMQP " +
                                        "maximum short string length of 255 characters.");
                }
                return typeName;
            }
            else {

                var typeName = type.FullName + ":" + type.Assembly.GetName().Name;
                if (typeName.Length > 255) {
                    throw new Exception("The serialized name of type exceeds the AMQP " +
                                        "maximum short string length of 255 characters.");
                }
                return typeName;
            }
        }
    }
}
