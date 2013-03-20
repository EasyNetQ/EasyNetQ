using System;

namespace EasyNetQ
{
    public class TypeNameSerializer
    {
        public static string Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");
            return type.FullName.Replace('.', '_') + ":" + type.Assembly.GetName().Name.Replace('.', '_');
        }
    }
}