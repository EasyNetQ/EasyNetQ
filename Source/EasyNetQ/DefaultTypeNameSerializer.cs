using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <inheritdoc />
    public class DefaultTypeNameSerializer : ITypeNameSerializer
    {
        private readonly ConcurrentDictionary<Type, string> serializedTypes = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<string, Type> deSerializedTypes = new ConcurrentDictionary<string, Type>();

        /// <inheritdoc />
        public string Serialize(Type type)
        {
            Preconditions.CheckNotNull(type, "type");

            return serializedTypes.GetOrAdd(type, t =>
            {
                var typeName = RemoveAssemblyDetails(t.AssemblyQualifiedName);
                if (typeName.Length > 255)
                {
                    throw new EasyNetQException($"The serialized name of type '{t.Name}' exceeds the AMQP maximum short string length of 255 characters");
                }
                return typeName;
            });
        }

        /// <inheritdoc />
        public Type DeSerialize(string typeName)
        {
            Preconditions.CheckNotBlank(typeName, "typeName");

            return deSerializedTypes.GetOrAdd(typeName, t =>
            {
                var typeNameKey = SplitFullyQualifiedTypeName(t);
                return GetTypeFromTypeNameKey(typeNameKey);
            });
        }

        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var builder = new StringBuilder(fullyQualifiedTypeName.Length);

            // loop through the type name and filter out qualified assembly details from nested type names
            var writingAssemblyName = false;
            var skippingAssemblyDetails = false;
            foreach (var character in fullyQualifiedTypeName)
            {
                switch (character)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(character);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(character);
                        break;
                    case ',':
                        if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(character);
                        }
                        else
                        {
                            skippingAssemblyDetails = true;
                        }
                        break;
                    default:
                        if (!skippingAssemblyDetails)
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private static TypeNameKey SplitFullyQualifiedTypeName(string fullyQualifiedTypeName)
        {
            var assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);

            string typeName;
            string assemblyName;

            if (assemblyDelimiterIndex != null)
            {
                typeName = fullyQualifiedTypeName.Trim(0, assemblyDelimiterIndex.GetValueOrDefault());
                assemblyName = fullyQualifiedTypeName.Trim(assemblyDelimiterIndex.GetValueOrDefault() + 1, fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1);
            }
            else
            {
                typeName = fullyQualifiedTypeName;
                assemblyName = null;
            }

            return new TypeNameKey(assemblyName, typeName);
        }

        private static Type GetTypeFromTypeNameKey(TypeNameKey typeNameKey)
        {
            var assemblyName = typeNameKey.AssemblyName;
            var typeName = typeNameKey.TypeName;

            if (assemblyName != null)
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                if (assembly == null)
                {
                    throw new EasyNetQException($"Could not load assembly '{assemblyName}'");
                }

                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    // if generic type, try manually parsing the type arguments for the case of dynamically loaded assemblies
                    // example generic typeName format: System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
                    if (typeName.IndexOf('`') >= 0)
                    {
                        try
                        {
                            type = GetGenericTypeFromTypeName(typeName, assembly);
                        }
                        catch (Exception ex)
                        {
                            throw new EasyNetQException($"Could not find type '{typeName}' in assembly '{assembly.FullName}'", ex);
                        }
                    }

                    if (type == null)
                    {
                        throw new EasyNetQException($"Could not find type '{typeName}' in assembly '{assembly.FullName}'");
                    }
                }

                return type;
            }

            return Type.GetType(typeName);
        }

        private static Type GetGenericTypeFromTypeName(string typeName, Assembly assembly)
        {
            Type type = null;
            var openBracketIndex = typeName.IndexOf('[');
            if (openBracketIndex >= 0)
            {
                var genericTypeDefName = typeName.Substring(0, openBracketIndex);
                var genericTypeDef = assembly.GetType(genericTypeDefName);
                if (genericTypeDef != null)
                {
                    var genericTypeArguments = new List<Type>();
                    var scope = 0;
                    var typeArgStartIndex = 0;
                    var endIndex = typeName.Length - 1;
                    for (var i = openBracketIndex + 1; i < endIndex; ++i)
                    {
                        var current = typeName[i];
                        switch (current)
                        {
                            case '[':
                                if (scope == 0)
                                {
                                    typeArgStartIndex = i + 1;
                                }
                                ++scope;
                                break;
                            case ']':
                                --scope;
                                if (scope == 0)
                                {
                                    var typeArgAssemblyQualifiedName = typeName.Substring(typeArgStartIndex, i - typeArgStartIndex);
                                    var typeNameKey = SplitFullyQualifiedTypeName(typeArgAssemblyQualifiedName);
                                    genericTypeArguments.Add(GetTypeFromTypeNameKey(typeNameKey));
                                }
                                break;
                        }
                    }

                    type = genericTypeDef.MakeGenericType(genericTypeArguments.ToArray());
                }
            }

            return type;
        }

        private static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
        {
            // we need to get the first comma following all surrounded in brackets because of generic types
            // e.g. System.Collections.Generic.Dictionary`2[[System.String, mscorlib,Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
            var scope = 0;
            for (var i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                var current = fullyQualifiedTypeName[i];
                switch (current)
                {
                    case '[':
                        scope++;
                        break;
                    case ']':
                        scope--;
                        break;
                    case ',':
                        if (scope == 0)
                        {
                            return i;
                        }
                        break;
                }
            }

            return null;
        }

        private readonly struct TypeNameKey
        {
            public string AssemblyName { get; }
            public string TypeName { get; }

            public TypeNameKey(string assemblyName, string typeName)
            {
                AssemblyName = assemblyName;
                TypeName = typeName;
            }
        }
    }
}
