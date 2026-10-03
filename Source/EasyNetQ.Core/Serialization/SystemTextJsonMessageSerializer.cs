using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Diagnostics.CodeAnalysis;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.SystemTextJson;

/// <summary>
///     The default <see cref="IMessageSerializer" />: System.Text.Json working through
///     <see cref="JsonTypeInfo{T}" /> contracts cached on the message type descriptor. With a
///     <see cref="JsonSerializerContext" /> supplied, serialization is reflection-free and AOT-safe.
/// </summary>
public sealed class SystemTextJsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions options;

    /// <summary>
    ///     The default options: general defaults with case-insensitive property matching, so camelCase peers
    ///     (ASP.NET Core, Wolverine, most non-.NET producers) deserialize instead of silently producing defaults.
    ///     Writing keeps the property names as declared.
    /// </summary>
    public static JsonSerializerOptions CreateDefaultOptions()
        => new(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true };

    /// <summary>
    ///     Creates the serializer with the default options (<see cref="CreateDefaultOptions" />)
    /// </summary>
    public SystemTextJsonMessageSerializer()
        : this(CreateDefaultOptions())
    {
    }

    /// <summary>
    ///     Creates the serializer with custom options
    /// </summary>
    public SystemTextJsonMessageSerializer(JsonSerializerOptions options)
        : this(options, null)
    {
    }

    /// <summary>
    ///     Creates the serializer with custom options plus additional converters (e.g. the transport package's
    ///     MessageProperties converter, registered as JsonConverter services)
    /// </summary>
    public SystemTextJsonMessageSerializer(JsonSerializerOptions options, IEnumerable<JsonConverter>? extraConverters)
    {
        this.options = new JsonSerializerOptions(options);
        if (extraConverters is not null)
            foreach (var converter in extraConverters)
                this.options.Converters.Add(converter);
        // reflection-based contracts only where they work; under Native AOT an unknown type fails at use with
        // "no metadata provided" - pass a JsonSerializerContext (UseSystemTextJson(context)) there
        this.options.TypeInfoResolver ??= DefaultResolver();
        this.options.MakeReadOnly();
    }

    /// <summary>
    ///     Creates the serializer with a source-generated contract context (reflection-free, AOT-safe)
    /// </summary>
    public SystemTextJsonMessageSerializer(JsonSerializerContext context)
        : this((IJsonTypeInfoResolver)context)
    {
    }

    /// <summary>
    ///     Creates the serializer with an explicit contract resolver, e.g. several source-generated contexts
    ///     combined via <see cref="JsonTypeInfoResolver.Combine" /> (reflection-free, AOT-safe)
    /// </summary>
    public SystemTextJsonMessageSerializer(IJsonTypeInfoResolver resolver)
        : this(resolver, null)
    {
    }

    /// <summary>
    ///     Creates the serializer with an explicit contract resolver plus additional converters (reflection-free,
    ///     AOT-safe)
    /// </summary>
    public SystemTextJsonMessageSerializer(IJsonTypeInfoResolver resolver, IEnumerable<JsonConverter>? extraConverters)
    {
        options = CreateDefaultOptions();
        options.TypeInfoResolver = resolver;
        if (extraConverters is not null)
            foreach (var converter in extraConverters)
                options.Converters.Add(converter);
        options.MakeReadOnly();
    }

    /// <summary>
    ///     The default contract resolver: the registered source-generated <paramref name="contexts" /> (the
    ///     application's and the transport's), then reflection-based contracts where runtime reflection is available.
    ///     Under Native AOT a type no context covers fails at use with "no metadata provided".
    /// </summary>
    public static IJsonTypeInfoResolver CreateDefaultResolver(IEnumerable<IJsonTypeInfoResolver> contexts)
        => JsonTypeInfoResolver.Combine([.. contexts, DefaultResolver()]);

    private static IJsonTypeInfoResolver DefaultResolver()
    {
#if NET
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported || !RuntimeReflection.IsSupported)
#else
        if (!RuntimeReflection.IsSupported)
#endif
            return JsonTypeInfoResolver.Combine();
        return ReflectionResolver();
    }

    [RequiresUnreferencedCode("Reflection-based JSON contracts")]
    [RequiresDynamicCode("Reflection-based JSON contracts")]
    private static IJsonTypeInfoResolver ReflectionResolver() => new DefaultJsonTypeInfoResolver();

    /// <inheritdoc />
    public IMemoryOwner<byte> Serialize<T>(T body, MessageTypeDescriptor<T> descriptor)
    {
        var stream = new ArrayPooledMemoryStream();
        JsonSerializer.Serialize(stream, body, GetTypeInfo(descriptor));
        return stream;
    }

    /// <inheritdoc />
    public T? Deserialize<T>(in ReadOnlyMemory<byte> body, MessageTypeDescriptor<T> descriptor)
        => JsonSerializer.Deserialize(body.Span, GetTypeInfo(descriptor));

    private JsonTypeInfo<T> GetTypeInfo<T>(MessageTypeDescriptor<T> descriptor)
    {
        // benign race: concurrent writers store the same JsonTypeInfo for this serializer's options
        if (descriptor.SerializerState is JsonTypeInfo<T> cached)
            return cached;

        var info = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        descriptor.SerializerState = info;
        return info;
    }
}
