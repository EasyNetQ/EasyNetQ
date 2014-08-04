using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EasyNetQ
{
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<Type, Attribute[]> Attributes = new ConcurrentDictionary<Type, Attribute[]>();
    
        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Type type)
        {
            return Attributes.GetOrAdd(type, t => t.GetCustomAttributes(true)
                                                   .Cast<Attribute>()
                                                   .ToArray())
                             .Where(x => x.GetType() == typeof (TAttribute))
                             .Cast<TAttribute>()
                             .ToArray();
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
                    var constructorInfo = typeof (T).GetConstructor(new Type[0]);
                    if (constructorInfo == null)
                        throw new Exception();
                    factory = Expression.Lambda<Func<T>>(Expression.New(constructorInfo)).Compile();
                }
                return factory();
            }
        }
    }
}