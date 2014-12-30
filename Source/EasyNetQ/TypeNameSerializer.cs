using System;
using System.Linq;

namespace EasyNetQ
{
    public interface ITypeNameSerializer
    {
        string Serialize(Type type);
        Type DeSerialize(string typeName);
    }

    //avoid breaking existing implementations of ITypeNameSerializer
    public interface IFriendlyTypeNameSerializer
    {
      string SerializeFriendly(Type type);
    }

    public static class ITypeNameSerializerExtensions
    {
      public static string SerializeFriendly(this ITypeNameSerializer serializer, Type type)
      {
        var friendlySerializer = serializer as IFriendlyTypeNameSerializer;
        
        if (friendlySerializer != null)
          return friendlySerializer.SerializeFriendly(type);

        return serializer.Serialize(type);
      }
    }

    public class TypeNameSerializer : ITypeNameSerializer, IFriendlyTypeNameSerializer
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

      public string SerializeFriendly(Type type)
      {
        const string GENERIC_FORMAT = "<{0}>";

        var friendlyName = type.Name;

        if (!type.IsGenericType)
        {
          var backtickIndex = friendlyName.IndexOf('`');
          if (backtickIndex > 0)
            friendlyName = friendlyName.Remove(backtickIndex);

          var genericArguments = type.GetGenericArguments().Select(a => a.Name);
          var genericArgumentNames = String.Join(",", genericArguments);

          friendlyName += String.Format(GENERIC_FORMAT, genericArgumentNames);
        }

        return friendlyName;
      }
    }
}
