using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.SystemTextJson;

public sealed class SystemTextJsonSerializerV2 : ISerializer
{
    private readonly System.Text.Json.JsonSerializerOptions options;

    [RequiresUnreferencedCode(Compat.ReflectionApi)]
    [RequiresDynamicCode(Compat.ReflectionApi)]
    public SystemTextJsonSerializerV2()
        : this(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.General))
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    [RequiresUnreferencedCode(Compat.ReflectionApi)]
    [RequiresDynamicCode(Compat.ReflectionApi)]
    public SystemTextJsonSerializerV2(System.Text.Json.JsonSerializerOptions options)
    {
        this.options = new System.Text.Json.JsonSerializerOptions(options);
        this.options.Converters.Add(new MessagePropertiesConverter());
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = Compat.Annotated)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = Compat.Annotated)]
    public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
    {
        var stream = new ArrayPooledMemoryStream();
        System.Text.Json.JsonSerializer.Serialize(stream, message, messageType, options);
        return stream;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = Compat.Annotated)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = Compat.Annotated)]
    public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
    {
        return System.Text.Json.JsonSerializer.Deserialize(bytes.Span, messageType, options)!;
    }
}
