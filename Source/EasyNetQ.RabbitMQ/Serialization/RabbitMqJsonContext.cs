using System.Text.Json.Serialization;
using EasyNetQ.SystemMessages;

namespace EasyNetQ.Serialization.SystemTextJson;

/// <summary>
///     Source-generated contracts for the transport's own messages (the error queue's <see cref="Error" />), so the
///     error strategy works without reflection. Registered as a <see cref="JsonSerializerContext" /> service, which
///     the default serializer combines with the application's contexts.
/// </summary>
[JsonSourceGenerationOptions(Converters = [typeof(MessagePropertiesConverter)])]
[JsonSerializable(typeof(Error))]
internal sealed partial class RabbitMqJsonContext : JsonSerializerContext;
