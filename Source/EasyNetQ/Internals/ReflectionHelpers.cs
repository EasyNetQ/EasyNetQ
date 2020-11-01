using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>> typesAttributes = new ConcurrentDictionary<Type, Dictionary<Type, Attribute[]>>();

        private static Dictionary<Type, Attribute[]> GetOrAddTypeAttributeDictionary(Type type)
        {
            return typesAttributes.GetOrAdd(type, t => t.GetCustomAttributes(true)
                                                    .Cast<Attribute>()
                                                    .GroupBy(attr => attr.GetType())
                                                    .ToDictionary(group => group.Key, group => group.ToArray()));
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Type type) where TAttribute : Attribute
        {
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out var attributes))
            {
                return attributes.Cast<TAttribute>();
            }
            return new TAttribute[0];
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static TAttribute GetAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out var attributes) && attributes.Length > 0)
            {
                return (TAttribute)attributes[0];
            }
            return default;
        }
    }
}
