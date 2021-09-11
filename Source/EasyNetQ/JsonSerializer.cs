using System;
using System.Buffers;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    public class JsonSerializer : ISerializer
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);

        private const int DefaultBufferSize = 1024;
        private readonly object jsonSerializer;
        private readonly Action<object, object, object, Type> serializeFunc;
        private readonly Func<object, object, Type, object> deserializeFunc;
        private readonly Func<StreamWriter, object, IDisposable> createJsonWriterFunc;
        private readonly Func<StreamReader, IDisposable> createJsonReaderFunc;

        /// <summary>
        ///     Creates JsonSerializer
        /// </summary>
        public JsonSerializer()
        {
            var jsonSerializerSettingsType = FindType("Newtonsoft.Json.JsonSerializerSettings", "Newtonsoft.Json");
            var typeNameHandlingType = FindType("Newtonsoft.Json.TypeNameHandling", "Newtonsoft.Json");
            var jsonSerializerType = FindType("Newtonsoft.Json.JsonSerializer", "Newtonsoft.Json");
            var jsonTextWriterType = FindType("Newtonsoft.Json.JsonTextWriter", "Newtonsoft.Json");
            var jsonTextReaderType = FindType("Newtonsoft.Json.JsonTextReader", "Newtonsoft.Json");

            var jsonSerializerSettings = Activator.CreateInstance(jsonSerializerSettingsType!);
            jsonSerializerSettingsType.GetProperty("TypeNameHandling")!
                .SetValue(jsonSerializerSettings, Enum.Parse(typeNameHandlingType!, "Auto", false));
            jsonSerializer = jsonSerializerType!.GetMethod("Create", new [] {jsonSerializerSettingsType})!
                .Invoke(null, new[] {jsonSerializerSettings});

            {
                var streamWriterParameter = Expression.Parameter(typeof(StreamWriter), "streamWriter");
                var jsonSerializerParameter = Expression.Parameter(typeof(object), "jsonSerializer");
                var jsonTextWriterParameter = Expression.Parameter(jsonTextWriterType!, "jsonTextWriter");
                var createJsonWriterLambda = Expression.Lambda<Func<StreamWriter, object, IDisposable>>(
                    Expression.Block(
                        jsonTextWriterType,
                        new[] {jsonTextWriterParameter},
                        Expression.Assign(
                            jsonTextWriterParameter,
                            Expression.New(
                                jsonTextWriterType!.GetConstructor(new[] {typeof(StreamWriter)})!,
                                streamWriterParameter
                            )
                        ),
                        Expression.Assign(
                            Expression.MakeMemberAccess(
                                jsonTextWriterParameter, jsonTextWriterType.GetProperty("Formatting")!
                            ),
                            Expression.MakeMemberAccess(
                                Expression.Convert(jsonSerializerParameter, jsonSerializerType),
                                jsonSerializerType.GetProperty("Formatting")!
                            )
                        ),
                        jsonTextWriterParameter
                    ),
                    streamWriterParameter,
                    jsonSerializerParameter
                );
                createJsonWriterFunc = createJsonWriterLambda.Compile();
            }

            {
                var jsonSerializerParameter = Expression.Parameter(typeof(object), "jsonSerializer");
                var jsonTextWriterParameter = Expression.Parameter(typeof(object), "jsonTextWriter");
                var messageParameter = Expression.Parameter(typeof(object), "message");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var serializeLambda = Expression.Lambda<Action<object, object, object, Type>>(
                    Expression.Call(
                        Expression.Convert(jsonSerializerParameter, jsonSerializerType),
                        jsonSerializerType.GetMethod("Serialize", new [] {jsonTextWriterType, typeof(object), typeof(Type)})!,
                        Expression.Convert(jsonTextWriterParameter, jsonTextWriterType!),
                        messageParameter,
                        messageTypeParameter
                    ),
                    jsonSerializerParameter,
                    jsonTextWriterParameter,
                    messageParameter,
                    messageTypeParameter
                );
                serializeFunc = serializeLambda.Compile();
            }
            
            {
                var streamReaderParameter = Expression.Parameter(typeof(StreamReader), "streamReader");
                var createJsonReaderLambda = Expression.Lambda<Func<StreamReader ,IDisposable>>(
                    Expression.New(
                        jsonTextReaderType!.GetConstructor(new[] {typeof(StreamReader)})!,
                        streamReaderParameter
                    ),
                    streamReaderParameter
                );
                createJsonReaderFunc = createJsonReaderLambda.Compile();
            }

            {
                var jsonSerializerParameter = Expression.Parameter(typeof(object), "jsonSerializer");
                var jsonTextReaderParameter = Expression.Parameter(typeof(object), "jsonTextReader");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var serializeLambda = Expression.Lambda<Func<object, object, Type, object>>(
                    Expression.Call(
                        Expression.Convert(jsonSerializerParameter, jsonSerializerType),
                        jsonSerializerType.GetMethod("Deserialize", new[] {jsonTextReaderType, typeof(Type)})!,
                        Expression.Convert(jsonTextReaderParameter, jsonTextReaderType!),
                        messageTypeParameter
                    ),
                    jsonSerializerParameter,
                    jsonTextReaderParameter,
                    messageTypeParameter
                );
                deserializeFunc = serializeLambda.Compile();
            }
        }

        /// <inheritdoc />
        public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
        {
            Preconditions.CheckNotNull(messageType, nameof(messageType));

            var stream = new ArrayPooledMemoryStream();

            using var streamWriter = new StreamWriter(stream, Encoding, DefaultBufferSize, true);
            using var jsonWriter = createJsonWriterFunc(streamWriter, jsonSerializer);
            serializeFunc(jsonSerializer, jsonWriter, message, messageType);
            return stream;
        }

        /// <inheritdoc />
        public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
        {
            Preconditions.CheckNotNull(messageType, nameof(messageType));

            using var memoryStream = new ReadOnlyMemoryStream(bytes);
            using var streamReader = new StreamReader(memoryStream, Encoding, false, DefaultBufferSize, true);
            using var reader = createJsonReaderFunc(streamReader);
            return deserializeFunc(jsonSerializer, reader, messageType);
        }
        
        private static Type FindType(string typeName, string assemblyName)
        {
            return Type.GetType($"{typeName}, {assemblyName}") ?? Type.GetType(typeName);
        }
    }
}