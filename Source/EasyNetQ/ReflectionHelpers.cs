using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


namespace EasyNetQ
{
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>> typesAttributes = new ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>>();

        private static Dictionary<Type, Attribute[]> GetOrAddTypeAttributeDictionary(Type type)
        {
            return typesAttributes.GetOrAdd(type, t => t.GetTypeInfo().GetCustomAttributes(true)
                                                    .Cast<Attribute>()
                                                    .GroupBy(attr => attr.GetType())
                                                    .ToDictionary(group => group.Key, group => group.ToArray()));
        }

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Type type) where TAttribute : Attribute
        {
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out var attributes))
            {
                return attributes.Cast<TAttribute>();
            }
            return new TAttribute[0];
        }

        public static TAttribute GetAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            Attribute[] attributes;
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out attributes) && attributes.Length > 0)
            {
                return (TAttribute)attributes[0];
            }
            return default(TAttribute);
        }
    }
}