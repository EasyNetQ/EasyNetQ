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
            return Type.GetType(nameParts[0]);
        }

        string ITypeNameSerializer.Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");
            return type.FullName + ":" + type.Assembly.GetName().Name;
        }
    }
}