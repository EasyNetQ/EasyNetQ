using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EasyNetQ.Configuration;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class InterceptorScenarios
{
    private static readonly byte[] Key = Enumerable.Range(1, 24).Select(i => (byte)(i * 7)).ToArray();
    private static readonly byte[] Iv = Enumerable.Range(1, 8).Select(i => (byte)(i * 13)).ToArray();

    /// <summary>
    ///     GZip + TripleDES interceptors on the bus-wide pipelines, a custom compression step after SerializeStep on a
    ///     fluent publish route (and its inverse before DeserializeStep), the wire format, and what the error queue keeps
    /// </summary>
    public static async Task InterceptorsAsync(ScenarioContext ctx)
    {
        const int count = 30;
        var errorQueue = ctx.Name("errors");
        var fluentExchange = ctx.Name("fluent");
        ctx.DeleteQueueLater(errorQueue);
        ctx.DeleteExchangeLater(errorQueue);
        ctx.DeleteExchangeLater(fluentExchange);

        var compressed = new Inbox<CompressedPayload>(ctx, p => p.Id.ToString());
        var decompressedHeaders = 0;
        var bus = ctx.Bus(
            b => b.UseRabbitMq(r => r
                .ErrorQueue(errorQueue)
                .Publish(p => p
                    .Exchange(fluentExchange, e => e.Topic())
                    .Message<CompressedPayload>("compressed")
                    .Pipeline(pipeline => pipeline.InsertAfter<SerializeStep, BrotliCompressStep>(_ => new BrotliCompressStep())))
                .Consume(c => c
                    .Queue(ctx.Name("fluent"), q => q.AutoDelete())
                    .Bind(fluentExchange, "#", e => e.Topic())
                    .Message(pipeline => pipeline.InsertBefore<DeserializeStep, BrotliDecompressStep>(_ => new BrotliDecompressStep(() => Interlocked.Increment(ref decompressedHeaders))))
                    .Handle<CompressedPayload>((message, _) =>
                    {
                        if (message.Run == ctx.Run) compressed.Add(message);
                        return new ValueTask<AckDecision>(AckDecision.Ack);
                    }))),
            services: s => s
                .AddSingleton<IProduceConsumeInterceptor>(new GZipInterceptor())
                .AddSingleton<IProduceConsumeInterceptor>(new TripleDESInterceptor(Key, Iv))
                .AddSingleton<IErrorMessageSerializer, Base64ErrorMessageSerializer>()
        );
        // no interceptors: sees the wire bytes
        var plainBus = ctx.Bus();
        var conventions = bus.Provider.GetRequiredService<IConventions>();

        var payloads = new Inbox<Payload>(ctx, p => p.Id.ToString());
        await using var subscription = await bus.Bus.PubSub.SubscribeAsync<Payload>(
            ctx.Name("payloads"),
            (payload, _) =>
            {
                if (payload.Run != ctx.Run) return Task.CompletedTask;
                if (payload.Text == "fail") throw new InvalidOperationException($"soak failure {payload.Id}");
                payloads.Add(payload);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );

        var tap = await plainBus.Advanced.QueueDeclareAsync(ctx.Name("tap"), durable: true, exclusive: false, autoDelete: true, cancellationToken: ctx.Token);
        await plainBus.Advanced.BindAsync(new Exchange(conventions.ExchangeNamingConvention(typeof(Payload))), tap, "#", ctx.Token);
        var wire = new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
        await using var tapConsumer = await plainBus.Advanced.ConsumeAsync(tap, (body, _, _, _) =>
        {
            wire.Enqueue(body.ToArray());
            return Task.CompletedTask;
        });

        await bus.StartConsumersAsync(ctx.Token);

        var text = string.Concat(Enumerable.Repeat("compressible text ", 40));
        for (var i = 0; i < count; i++)
        {
            await bus.Bus.PubSub.PublishAsync(new Payload(ctx.Run, i, text), ctx.Token);
            await bus.Publisher.PublishAsync(new CompressedPayload(ctx.Run, i, text), ctx.Token);
            ctx.CountSent(2);
        }
        await bus.Bus.PubSub.PublishAsync(new Payload(ctx.Run, -1, "fail"), ctx.Token);
        ctx.CountSent();

        ctx.Check("PubSub through GZip + TripleDES received", await payloads.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({payloads.Count}/{count}, duplicates {payloads.Duplicates})");
        ctx.Check("PubSub payloads intact", payloads.Items.All(p => p.Text == text));

        await Soak.WaitUntilAsync(() => wire.Count >= count + 1, TimeSpan.FromSeconds(10), ctx.Token);
        var decoded = wire.Select(TryDecode).ToList();
        ctx.Check(
            "wire body is TripleDES(GZip(json)) (interceptors run in registration order)",
            decoded.Count >= count && decoded.All(json => json is not null && json.Contains(ctx.Run, StringComparison.Ordinal)),
            $"({decoded.Count(d => d is not null)}/{decoded.Count} decodable)"
        );

        ctx.Check("fluent route with a compression step after SerializeStep received", await compressed.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({compressed.Count}/{count})");
        ctx.Check("the consume-side step saw the compression header on every message", Volatile.Read(ref decompressedHeaders) >= count, $"({decompressedHeaders})");
        ctx.Check("fluent payloads intact", compressed.Items.All(p => p.Text == text));

        var errorArrived = await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(errorQueue, ctx.Token) >= 1, TimeSpan.FromSeconds(10), ctx.Token);
        ctx.Check("failed message reached the error queue", errorArrived);
        if (!errorArrived) return;
        var errors = await ctx.Admin.DrainAsync(errorQueue, ctx.Token);
        using var error = JsonDocument.Parse(errors[0].Body);
        var keptBody = Convert.FromBase64String(error.RootElement.GetProperty("Message").GetString()!);
        var keptJson = TryDecode(keptBody);
        ctx.Check(
            "error queue keeps the body as it was received (still encrypted + compressed), as 8.x did",
            keptJson is not null && keptJson.Contains("\"fail\"", StringComparison.Ordinal),
            keptJson is null ? $"(kept body is not the wire body: {Preview(keptBody)})" : ""
        );
    }

    private static string? TryDecode(byte[] wire)
    {
        try
        {
            using var tripleDes = TripleDES.Create();
            using var decryptor = tripleDes.CreateDecryptor(Key, Iv);
            var compressed = decryptor.TransformFinalBlock(wire, 0, wire.Length);
            using var input = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress);
            using var output = new MemoryStream();
            input.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static string Preview(byte[] bytes) => Encoding.UTF8.GetString(bytes.AsSpan(0, Math.Min(60, bytes.Length)));

    private const string EncodingHeader = "x-soak-encoding";

    private sealed class BrotliCompressStep : IMiddleware<PublishContext>
    {
        public async ValueTask InvokeAsync(PublishContext context, PipelineStep<PublishContext> next)
        {
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.Fastest, leaveOpen: true))
                brotli.Write(context.Body.Span);
            context.Body = output.ToArray();
            context.Properties = context.Properties.SetHeader(EncodingHeader, "br");
            await next(context);
        }
    }

    private sealed class BrotliDecompressStep(Action onCompressed) : IMiddleware<ConsumeContext>
    {
        public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
        {
            if (context.Properties.Headers?.TryGetValue(EncodingHeader, out var value) == true && value is byte[] raw && Encoding.UTF8.GetString(raw) == "br")
            {
                onCompressed();
                using var input = new BrotliStream(new MemoryStream(context.Body.ToArray()), CompressionMode.Decompress);
                using var output = new MemoryStream();
                input.CopyTo(output);
                context.Body = output.ToArray();
            }
            return next(context);
        }
    }
}
