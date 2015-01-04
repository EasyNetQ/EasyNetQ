using System;

namespace EasyNetQ
{
    public interface ITypeNameSerializer
    {
        string Serialize(Type type);
        Type DeSerialize(string typeName);
    }

    public class TypeNameSerializer : ITypeNameSerializer
    {
        public Type DeSerialize(string typeName)
        {
            var nameParts = typeName.Split(':');
            if (nameParts.Length != 2)
            {
                throw new EasyNetQException(
                    "type name {0}, is not a valid EasyNetQ type name. Expected Type:Assembly", 
                    typeName);
            }
            var type = Type.GetType(nameParts[0] + ", " + nameParts[1]);
            if (type == null)
            {
                throw new EasyNetQException(
                    "Cannot find type {0}",
                    typeName);
            }
            return type;
        }

        public string Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");
            var typeName = type.FullName + ":" + type.Assembly.GetName().Name;
            if (typeName.Length > 255)
            {
                throw new EasyNetQException("The serialized name of type '{0}' exceeds the AMQP " + 
                    "maximum short string length of 255 characters.",
                    type.Name);
            }
            return typeName;
        }
    }
}
