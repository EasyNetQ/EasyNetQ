using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EasyNetQ
{
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>> _attributes = new ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>>();

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Type type) where TAttribute : Attribute
        {
            var typeAttributeDictionary = _attributes.GetOrAdd(type, t => t.GetCustomAttributes(true)
                                                              .Cast<Attribute>()
                                                              .GroupBy(attr => attr.GetType())
                                                              .ToDictionary(group => group.Key, group => group.ToArray()));
            Attribute[] attributes;
            return typeAttributeDictionary.TryGetValue(typeof(TAttribute), out attributes) ? attributes.Cast<TAttribute>().ToArray() : new TAttribute[0];

        }

        public static TAttribute GetAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            var typeAttributeDictionary = _attributes.GetOrAdd(type, t => t.GetCustomAttributes(true)
                                                                           .Cast<Attribute>()
                                                                           .GroupBy(attr => attr.GetType())
                                                                           .ToDictionary(group => group.Key, group => group.ToArray()));
            Attribute[] attributes;
            if (typeAttributeDictionary.TryGetValue(typeof(TAttribute), out attributes) && attributes.Length > 0)
            {
                return (TAttribute)attributes[0];
            }
            return default(TAttribute);
        }

        public static T CreateInstance<T>()
        {
            return DefaultFactories<T>.Get();
        }

        private static class DefaultFactories<T>
        {
            private static Func<T> factory;

            public static T Get()
            {
                if (factory == null)
                {
                    var constructorInfo = typeof(T).GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null)
                    {
                        throw new MissingMethodException("The type that is specified for T does not have a parameterless constructor.");
                    }
                    factory = Expression.Lambda<Func<T>>(Expression.New(constructorInfo)).Compile();
                }
                return factory();
            }
        }
    }
}