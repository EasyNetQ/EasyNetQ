using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EasyNetQ
{
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

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Type type) where TAttribute : Attribute
        {
            Attribute[] attributes;
            if (GetOrAddTypeAttributeDictionary(type).TryGetValue(typeof(TAttribute), out attributes))
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

        /// <summary>
        /// A factory method that creates an instance of <paramref name="{T}"/> using a public parameterless constructor.
        /// If no such constructor is found on <paramref name="{T}"/>, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
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
                        throw new MissingMethodException("The type that is specified for T does not have a public parameterless constructor.");
                    }
                    factory = Expression.Lambda<Func<T>>(Expression.New(constructorInfo)).Compile();
                }
                return factory();
            }
        }

        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Func<object, object>>> singleParameterConstructorMap = 
            new ConcurrentDictionary<Type, Dictionary<Type, Func<object, object>>>();
        private static readonly Func<Type, Type, Func<object, object>> singleParameterConstructorMapUpdate = ((objectType, argType) =>
        {
            var ctor = objectType.GetConstructor(new[] { argType });
            if (ctor == null)
            {
                throw new MissingMethodException(String.Format("Type {0} doesn't have a public constructor that take one parameter of type {1}."
                                                               , objectType, argType));
            }
            var paramExp = Expression.Parameter(typeof(object), "arg");
            return Expression.Lambda<Func<object, object>>(Expression.New(ctor, new Expression[] { Expression.Convert(paramExp, argType) }), paramExp).Compile();
        });

        /// <summary>
        /// A factory method that creates an instance of the specified <see cref="Type"/>
        /// using a public constructor that accepts one argument of the type of <paramref name="arg"/>.
        /// If no such constructor is found on type of <paramref name="objectType"/>, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
        public static object CreateInstance(Type objectType, object arg)
        {
            var argType = arg.GetType();
            var constructors = singleParameterConstructorMap.GetOrAdd(objectType, t => new Dictionary<Type, Func<object, object>>
            {
                { argType, singleParameterConstructorMapUpdate(objectType, argType) }
            });

            Func<object, object> ctor;
            if (!constructors.TryGetValue(argType, out ctor))
            {
                ctor = singleParameterConstructorMapUpdate(objectType, argType);
                constructors.Add(argType, ctor);
            }
            return ctor(arg);
        }

        private static readonly ConcurrentDictionary<Type, Dictionary<Type, Dictionary<Type, Func<object, object, object>>>> dualParameterConstructorMap =
            new ConcurrentDictionary<Type, Dictionary<Type, Dictionary<Type, Func<object, object, object>>>>();
        private static readonly Func<Type, Type, Type, Func<object, object, object>> dualParameterConstructorMapUpdate = ((objectType, firstArgType, secondArgType) =>
        {
            var ctor = objectType.GetConstructor(new[] { firstArgType, secondArgType });
            if (ctor == null)
            {
                throw new MissingMethodException(String.Format("Type {0} doesn't have a public constructor that take two parametesr of type {1} and {2}.",
                                                               objectType, firstArgType, secondArgType));
            }
            var paramExp1 = Expression.Parameter(typeof(object), "firstArg");
            var paramExp2 = Expression.Parameter(typeof(object), "secondArg");
            var argsExp = new Expression[2];
            argsExp[0] = Expression.Convert(paramExp1, firstArgType);
            argsExp[1] = Expression.Convert(paramExp2, secondArgType);
            return Expression.Lambda<Func<object, object, object>>(Expression.New(ctor, argsExp), paramExp1, paramExp2).Compile();
        });

        /// <summary>
        /// A factory method that creates an instance of the specified <see cref="Type"/>
        /// using a public constructor that accepts two arguments of the type of <paramref name="firstArg"/> and <paramref name="secondArg"/> in that order.
        /// If no such constructor is found on type of <paramref name="objectType"/>, a <see cref="MissingMethodException"/> will be thrown.
        /// </summary>
        public static object CreateInstance(Type objectType, object firstArg, object secondArg)
        {
            var firstArgType = firstArg.GetType();
            var secondArgType = secondArg.GetType();

            var constructors = dualParameterConstructorMap.GetOrAdd(objectType, t => new Dictionary<Type, Dictionary<Type, Func<object, object, object>>>
            {
                {
                    firstArgType, new Dictionary<Type, Func<object, object, object>>
                    {
                        { secondArgType, dualParameterConstructorMapUpdate(objectType, firstArgType, secondArgType) }
                    }
                }
            });

            Dictionary<Type, Func<object, object, object>> firstArgConstructorMap;
            if (!constructors.TryGetValue(firstArgType, out firstArgConstructorMap))
            {
                firstArgConstructorMap = new Dictionary<Type, Func<object, object, object>>
                {
                    { secondArgType, dualParameterConstructorMapUpdate(objectType, firstArgType, secondArgType) }
                };
                constructors.Add(firstArgType, firstArgConstructorMap);
            }

            Func<object, object, object> ctor;
            if (!firstArgConstructorMap.TryGetValue(secondArgType, out ctor))
            {
                ctor = dualParameterConstructorMapUpdate(objectType, firstArgType, secondArgType);
                firstArgConstructorMap.Add(secondArgType, ctor);
            }
            return ctor(firstArg, secondArg);
        }
    }
}