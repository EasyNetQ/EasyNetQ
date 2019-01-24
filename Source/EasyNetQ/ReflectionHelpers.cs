using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public static T CreateObject<T>()
        {
            var type = typeof(T);

            if (type == typeof(string))
                return (T)(object)string.Empty;

            if (type.IsValueType)
                return Activator.CreateInstance<T>();

            if (type.IsArray)
                return (T)(object)Array.CreateInstance(type.GetElementType(), 0);

            var ctor = typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Count()).First();
            object[] values = ctor.GetParameters().Select(p => GetDefaultValue(p.ParameterType)).ToArray();
            return (T)Activator.CreateInstance(type, values);
        }

        private static object GetDefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}