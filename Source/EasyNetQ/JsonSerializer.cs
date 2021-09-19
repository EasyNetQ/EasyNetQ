using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     JsonSerializer based on Newtonsoft.Json which uses it dynamically
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);

        private const int DefaultBufferSize = 1024;
        private readonly object jsonSerializer;
        private readonly Action<object, object, object, Type> serialize;
        private readonly Func<object, object, Type, object> deserialize;
        private readonly Func<StreamWriter, object, IDisposable> createJsonWriter;
        private readonly Func<StreamReader, IDisposable> createJsonReader;

        /// <summary>
        ///     Creates JsonSerializer
        /// </summary>
        public JsonSerializer(object serializerSettings = null)
        {
            var jsonSerializerType = TryGetType("Newtonsoft.Json.JsonSerializer", "Newtonsoft.Json");
            if (jsonSerializerType == null)
            {
                throw new InvalidOperationException(
                    "Newtonsoft.Json assembly is not found. Starting from EasyNetQ v7, an explicit dependency from Newtonsoft.Json was removed. Please reference Newtonsoft.Json package (directly or indirectly) or add one of EasyNetQ.Serialization.* packages instead."
                );
            }

            var serializerSettingsType = GetType("Newtonsoft.Json.JsonSerializerSettings", "Newtonsoft.Json");
            var typeNameHandlingType = GetType("Newtonsoft.Json.TypeNameHandling", "Newtonsoft.Json");
            var textWriterType = GetType("Newtonsoft.Json.JsonTextWriter", "Newtonsoft.Json");
            var textReaderType = GetType("Newtonsoft.Json.JsonTextReader", "Newtonsoft.Json");

            if (serializerSettings == null)
            {
                serializerSettings = Activator.CreateInstance(serializerSettingsType);
                GetProperty(serializerSettingsType, "TypeNameHandling")
                    .SetValue(serializerSettings, Enum.Parse(typeNameHandlingType, "Auto", false));
            }
            else if (!serializerSettingsType.IsInstanceOfType(serializerSettings))
            {
                throw new InvalidOperationException("Incorrect settings type. Settings must be of Newtonsoft.Json.JsonSerializerSettings or derived type.");
            }

            jsonSerializer = GetMethod(jsonSerializerType, "Create", new[] { serializerSettingsType })
                .Invoke(null, new[] { serializerSettings });

            {
                var streamWriterParameter = Expression.Parameter(typeof(StreamWriter), "streamWriter");
                var jsonSerializerParameter = Expression.Parameter(typeof(object), "jsonSerializer");
                var jsonTextWriterParameter = Expression.Parameter(textWriterType, "jsonTextWriter");
                var createJsonWriterLambda = Expression.Lambda<Func<StreamWriter, object, IDisposable>>(
                    Expression.Block(
                        textWriterType,
                        new[] { jsonTextWriterParameter },
                        Expression.Assign(
                            jsonTextWriterParameter,
                            Expression.New(
                                GetConstructor(textWriterType, new[] { typeof(StreamWriter) }),
                                streamWriterParameter
                            )
                        ),
                        Expression.Assign(
                            Expression.MakeMemberAccess(
                                jsonTextWriterParameter, GetProperty(textWriterType, "Formatting")
                            ),
                            Expression.MakeMemberAccess(
                                Expression.Convert(jsonSerializerParameter, jsonSerializerType),
                                GetProperty(jsonSerializerType, "Formatting")
                            )
                        ),
                        jsonTextWriterParameter
                    ),
                    streamWriterParameter,
                    jsonSerializerParameter
                );
                createJsonWriter = createJsonWriterLambda.Compile();
            }

            {
                var jsonSerializerParameter = Expression.Parameter(typeof(object), "jsonSerializer");
                var jsonTextWriterParameter = Expression.Parameter(typeof(object), "jsonTextWriter");
                var messageParameter = Expression.Parameter(typeof(object), "message");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var serializeLambda = Expression.Lambda<Action<object, object, object, Type>>(
                    Expression.Call(
                        Expression.Convert(jsonSerializerParameter, jsonSerializerType),
                        GetMethod(
                            jsonSerializerType,
                            "Serialize",
                            new[] { textWriterType, typeof(object), typeof(Type) }
                        )!,
                        Expression.Convert(jsonTextWriterParameter, textWriterType),
                        messageParameter,
                        messageTypeParameter
                    ),
                    jsonSerializerParameter,
                    jsonTextWriterParameter,
                    messageParameter,
                    messageTypeParameter
                );
                serialize = serializeLambda.Compile();
            }

            {
                var streamReaderParameter = Expression.Parameter(typeof(StreamReader), "streamReader");
                var createJsonReaderLambda = Expression.Lambda<Func<StreamReader, IDisposable>>(
                    Expression.New(
                        GetConstructor(textReaderType, new[] { typeof(StreamReader) }), streamReaderParameter
                    ),
                    streamReaderParameter
                );
                createJsonReader = createJsonReaderLambda.Compile();
            }

            {
                var jsonSerializerParameter = Expression.Parameter(typeof(object), "jsonSerializer");
                var jsonTextReaderParameter = Expression.Parameter(typeof(object), "jsonTextReader");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var serializeLambda = Expression.Lambda<Func<object, object, Type, object>>(
                    Expression.Call(
                        Expression.Convert(jsonSerializerParameter, jsonSerializerType),
                        GetMethod(jsonSerializerType, "Deserialize", new[] { textReaderType, typeof(Type) }),
                        Expression.Convert(jsonTextReaderParameter, textReaderType),
                        messageTypeParameter
                    ),
                    jsonSerializerParameter,
                    jsonTextReaderParameter,
                    messageTypeParameter
                );
                deserialize = serializeLambda.Compile();
            }
        }

        /// <inheritdoc />
        public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
        {
            Preconditions.CheckNotNull(messageType, nameof(messageType));

            var stream = new ArrayPooledMemoryStream();

            using var streamWriter = new StreamWriter(stream, Encoding, DefaultBufferSize, true);
            using var jsonWriter = createJsonWriter(streamWriter, jsonSerializer);
            serialize(jsonSerializer, jsonWriter, message, messageType);
            return stream;
        }

        /// <inheritdoc />
        public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
        {
            Preconditions.CheckNotNull(messageType, nameof(messageType));

            using var memoryStream = new ReadOnlyMemoryStream(bytes);
            using var streamReader = new StreamReader(memoryStream, Encoding, false, DefaultBufferSize, true);
            using var reader = createJsonReader(streamReader);
            return deserialize(jsonSerializer, reader, messageType);
        }

        private static Type GetType(string typeName, string assemblyName)
        {
            return TryGetType(typeName, assemblyName)
                   ?? throw new InvalidOperationException($"Type {typeName} has not been found in {assemblyName}");
        }

        private static Type TryGetType(string typeName, string assemblyName)
        {
            return Type.GetType($"{typeName}, {assemblyName}") ?? Type.GetType(typeName);
        }

        private static MethodInfo GetMethod(Type type, string name, Type[] types)
        {
            return type.GetMethod(name, types)
                   ?? throw new InvalidOperationException($"Type {type} has no method {name}({string.Join(", ", types.Select(t => t.Name))})");
        }

        private static PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetProperty(name)
                   ?? throw new InvalidOperationException($"Type {type} has no property {name}");
        }

        private static ConstructorInfo GetConstructor(Type type, Type[] types)
        {
            return type.GetConstructor(types)
                   ?? throw new InvalidOperationException($"Type {type} has no public constructor with parameters {string.Join(", ", types.Select(t => t.Name))}");
        }
    }
}
