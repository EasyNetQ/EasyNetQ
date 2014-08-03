using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace EasyNetQ
{
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<Type, Attribute[]> Attributes = new ConcurrentDictionary<Type, Attribute[]>();
        private static readonly ConcurrentDictionary<Type, object> DefaultFactories = new ConcurrentDictionary<Type, object>();

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
            return ((Func<T>)DefaultFactories.GetOrAdd(typeof (T), type =>
                {
                    var constructorInfo = typeof (T).GetConstructor(new Type[0]);
                    if(constructorInfo == null)
                        throw new Exception();
                    return Expression.Lambda<Func<T>>(Expression.New(constructorInfo)).Compile();
                }))();
        }
    }
}