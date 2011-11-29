using System;

namespace EasyNetQ
{
    public class TypeNameSerializer
    {
        public static string Serialize(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return type.FullName.Replace('.', '_') + ":" + type.Assembly.GetName().Name.Replace('.', '_');
        }
    }
}