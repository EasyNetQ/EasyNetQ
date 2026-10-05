using System.Text;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class SerializationScenarios
{
    /// <summary>
    ///     UseNewtonsoftJson with TypeNameHandling.Auto: nested polymorphic members, decimals, dictionaries, offsets,
    ///     and a derived instance published through its base type
    /// </summary>
    public static async Task NewtonsoftAsync(ScenarioContext ctx)
    {
        const int count = 40;
        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        var bus = ctx.Bus(b => b.UseNewtonsoftJson(settings));
        var conventions = bus.Provider.GetRequiredService<IConventions>();
        var drawings = new Inbox<Drawing>(ctx, d => d.Id.ToString());
        var shapes = new Inbox<Shape>(ctx, s => s.Label);

        await using var drawingSubscription = await bus.Bus.PubSub.SubscribeAsync<Drawing>(
            ctx.Name("drawings"),
            (drawing, _) =>
            {
                if (drawing.Run == ctx.Run) drawings.Add(drawing);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );
        await using var shapeSubscription = await bus.Bus.PubSub.SubscribeAsync<Shape>(
            ctx.Name("shapes"),
            (shape, _) =>
            {
                if (shape.Label.StartsWith(ctx.Run, StringComparison.Ordinal)) shapes.Add(shape);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );

        // a raw tap on the Drawing exchange shows what Newtonsoft put on the wire
        var tap = await bus.Advanced.QueueDeclareAsync(ctx.Name("tap"), durable: true, exclusive: false, autoDelete: true, cancellationToken: ctx.Token);
        await bus.Advanced.BindAsync(new Exchange(conventions.ExchangeNamingConvention(typeof(Drawing))), tap, "#", ctx.Token);
        var rawBodies = new System.Collections.Concurrent.ConcurrentQueue<string>();
        await using var tapConsumer = await bus.Advanced.ConsumeAsync(tap, (body, _, _, _) =>
        {
            rawBodies.Enqueue(Encoding.UTF8.GetString(body.Span));
            return Task.CompletedTask;
        });

        var at = DateTimeOffset.UtcNow.AddHours(2).ToOffset(TimeSpan.FromHours(2));
        for (var i = 0; i < count; i++)
        {
            await bus.Bus.PubSub.PublishAsync(CreateDrawing(ctx.Run, i, at), ctx.Token);
            Shape shape = i % 2 == 0 ? new Circle { Label = $"{ctx.Run}.{i}", Radius = i + 0.5 } : new Square { Label = $"{ctx.Run}.{i}", Side = i };
            await bus.Bus.PubSub.PublishAsync(shape, ctx.Token);
            ctx.CountSent(2);
        }

        ctx.Check("drawings received", await drawings.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({drawings.Count}/{count}, duplicates {drawings.Duplicates})");
        var mismatches = drawings.Items.Where(d => !Matches(d, CreateDrawing(ctx.Run, d.Id, at))).Select(d => d.Id).ToList();
        ctx.Check("polymorphic members, decimal, dictionary, offset survive the round trip", mismatches.Count == 0, mismatches.Count == 0 ? "" : $"(ids {string.Join(",", mismatches.Take(5))})");

        ctx.Check("derived instances published as the base type received", await shapes.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({shapes.Count}/{count})");
        var wrongType = shapes.Items.Count(s =>
        {
            var id = int.Parse(s.Label[(s.Label.LastIndexOf('.') + 1)..]);
            return id % 2 == 0 ? s is not Circle { Radius: var r } || Math.Abs(r - (id + 0.5)) > 1e-9 : s is not Square { Side: var side } || Math.Abs(side - id) > 1e-9;
        });
        ctx.Check("runtime type of a base-typed publish is kept", wrongType == 0, $"({wrongType} wrong)");

        await Soak.WaitUntilAsync(() => rawBodies.Count >= count, TimeSpan.FromSeconds(10), ctx.Token);
        ctx.Check("wire body is Newtonsoft with $type for nested polymorphic members", rawBodies.Count >= count && rawBodies.All(b => b.Contains("\"$type\"", StringComparison.Ordinal)), $"({rawBodies.Count} tapped)");
    }

    private static Drawing CreateDrawing(string run, int id, DateTimeOffset at) => new()
    {
        Run = run,
        Id = id,
        Main = id % 3 == 0 ? new Circle { Label = "main", Radius = id } : new Square { Label = "main", Side = id * 2 },
        Shapes = Enumerable.Range(0, id % 4).Select(n => n % 2 == 0 ? (Shape)new Circle { Label = $"c{n}", Radius = n } : new Square { Label = $"s{n}", Side = n }).ToList(),
        Tags = new Dictionary<string, int> { ["id"] = id, ["mod"] = id % 7 },
        Amount = 1234567.890123m + id,
        At = at.AddSeconds(id),
    };

    private static bool Matches(Drawing actual, Drawing expected)
        => ShapeEquals(actual.Main, expected.Main)
           && actual.Shapes.Count == expected.Shapes.Count
           && actual.Shapes.Zip(expected.Shapes).All(p => ShapeEquals(p.First, p.Second))
           && actual.Tags.Count == expected.Tags.Count && actual.Tags.All(t => expected.Tags.TryGetValue(t.Key, out var v) && v == t.Value)
           && actual.Amount == expected.Amount
           && actual.At == expected.At && actual.At.Offset == expected.At.Offset;

    private static bool ShapeEquals(Shape? a, Shape? b) => (a, b) switch
    {
        (Circle x, Circle y) => x.Label == y.Label && x.Radius.Equals(y.Radius),
        (Square x, Square y) => x.Label == y.Label && x.Side.Equals(y.Side),
        (null, null) => true,
        _ => false,
    };
}
