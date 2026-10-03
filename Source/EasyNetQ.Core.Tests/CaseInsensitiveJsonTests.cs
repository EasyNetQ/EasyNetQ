using EasyNetQ.Hosting;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyNetQ.Configuration;
using EasyNetQ.Serialization.SystemTextJson;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public sealed record LocalEvent(int Id, string Product);

[JsonSerializable(typeof(LocalEvent))]
public sealed partial class CaseInsensitiveJsonContext : JsonSerializerContext;

public class CaseInsensitiveJsonTests
{
    private static readonly byte[] CamelCase = Encoding.UTF8.GetBytes("{\"id\":7,\"product\":\"socks\"}");

    [Fact]
    public void Should_read_camel_case_with_the_default_options()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());
        var serializer = new SystemTextJsonMessageSerializer();

        serializer.Deserialize<LocalEvent>(CamelCase, registry.GetOrAdd<LocalEvent>()).Should().Be(new LocalEvent(7, "socks"));
    }

    [Fact]
    public void Should_read_camel_case_with_a_source_generated_context()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());
        var serializer = new SystemTextJsonMessageSerializer(CaseInsensitiveJsonContext.Default);

        serializer.Deserialize<LocalEvent>(CamelCase, registry.GetOrAdd<LocalEvent>()).Should().Be(new LocalEvent(7, "socks"));
    }

    [Fact]
    public void Should_keep_declared_property_names_when_writing()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());
        using var body = new SystemTextJsonMessageSerializer().Serialize(new LocalEvent(1, "x"), registry.GetOrAdd<LocalEvent>());

        Encoding.UTF8.GetString(body.Memory.Span).Should().Be("{\"Id\":1,\"Product\":\"x\"}");
    }

    private sealed class MarkerSerializer : IMessageSerializer
    {
        public IMemoryOwner<byte> Serialize<T>(T message, MessageTypeDescriptor<T> descriptor) => throw new NotSupportedException();

        public T? Deserialize<T>(in ReadOnlyMemory<byte> bytes, MessageTypeDescriptor<T> descriptor)
            => (T)(object)new LocalEvent(42, "from consumer serializer");
    }

    [Fact]
    public async Task Should_deserialize_with_the_consumer_serializer()
    {
        var received = new TaskCompletionSource<LocalEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new InMemoryTransport();
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        services.AddEasyNetQCore().Consume(c => c
            .Queue("q")
            .Serializer(new MarkerSerializer())
            .Handle<LocalEvent>((message, _) =>
            {
                received.TrySetResult(message);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }));

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetServices<IHostedService>().Single();
        await host.StartAsync(TestContext.Current.CancellationToken);
        await provider.GetRequiredService<IConsumerHostStatus>().WaitForStartedAsync(TestContext.Current.CancellationToken);

        await WireNameTests.PublishRawAsync(transport, provider, "", "q", null, new LocalEvent(3, "x"));

        (await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().Be(new LocalEvent(42, "from consumer serializer"));
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
