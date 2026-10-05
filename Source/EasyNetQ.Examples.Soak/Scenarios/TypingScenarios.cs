using System.Text;
using EasyNetQ.MessageVersioning;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class TypingScenarios
{
    /// <summary>
    ///     UseVersionedMessage + ISupersede: an old (V1) subscriber receives V2 publishes, a V2 subscriber only V2, and a
    ///     message of a version nobody has loaded falls back to the first loadable alternative type
    /// </summary>
    public static async Task VersioningAsync(ScenarioContext ctx)
    {
        const int v2Count = 20, v1Count = 10, futureCount = 5;
        var bus = ctx.Bus(b => b.UseVersionedMessage());
        var oldInbox = new Inbox<OrderV1>(ctx, o => o.Id.ToString());
        var newInbox = new Inbox<OrderV2>(ctx, o => o.Id.ToString());

        await using var oldSubscription = await bus.Bus.PubSub.SubscribeAsync<OrderV1>(
            ctx.Name("old"),
            (order, _) =>
            {
                if (order.Run == ctx.Run) oldInbox.Add(order);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );
        await using var newSubscription = await bus.Bus.PubSub.SubscribeAsync<OrderV2>(
            ctx.Name("new"),
            (order, _) =>
            {
                if (order.Run == ctx.Run) newInbox.Add(order);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );

        for (var i = 0; i < v2Count; i++)
            await bus.Bus.PubSub.PublishAsync(new OrderV2 { Run = ctx.Run, Id = i, Customer = $"c{i}", Total = i * 1.5m + 1 }, ctx.Token);
        for (var i = 0; i < v1Count; i++)
            await bus.Bus.PubSub.PublishAsync(new OrderV1 { Run = ctx.Run, Id = 1000 + i, Customer = $"c{i}" }, ctx.Token);

        // a V3 published by a newer service: this process cannot load it, so it falls back to V2 (then V1)
        var typeNames = bus.Provider.GetRequiredService<ITypeNameSerializer>();
        var conventions = bus.Provider.GetRequiredService<IConventions>();
        var alternatives = Encoding.UTF8.GetBytes($"{typeNames.Serialize(typeof(OrderV2))};{typeNames.Serialize(typeof(OrderV1))}");
        for (var i = 0; i < futureCount; i++)
        {
            var properties = new MessageProperties { Type = "EasyNetQ.Examples.Soak.OrderV3, EasyNetQ.Examples.Soak.Future" }
                .SetHeader("Alternative-Message-Types", alternatives);
            var body = Encoding.UTF8.GetBytes($"{{\"Run\":\"{ctx.Run}\",\"Id\":{2000 + i},\"Customer\":\"f{i}\",\"Total\":9.5,\"Discount\":1}}");
            await bus.Advanced.PublishAsync(conventions.ExchangeNamingConvention(typeof(OrderV2)), "#", false, null, properties, (ReadOnlyMemory<byte>)body, ctx.Token);
        }
        ctx.CountSent(v2Count + v1Count + futureCount);

        var oldExpected = v2Count + v1Count + futureCount;
        var newExpected = v2Count + futureCount;
        ctx.Check("old (V1) subscriber receives V1, V2 and unknown-V3 publishes", await oldInbox.WaitForAsync(oldExpected, TimeSpan.FromSeconds(20), ctx.Token), $"({oldInbox.Count}/{oldExpected})");
        ctx.Check("new (V2) subscriber receives V2 and unknown-V3 publishes", await newInbox.WaitForAsync(newExpected, TimeSpan.FromSeconds(20), ctx.Token), $"({newInbox.Count}/{newExpected})");
        await Task.Delay(300, ctx.Token);
        ctx.Check("new (V2) subscriber does not receive V1 publishes", newInbox.Count == newExpected && newInbox.Items.All(o => o.Id < 1000 || o.Id >= 2000), $"({newInbox.Count})");
        var upgraded = oldInbox.Items.Where(o => o.Id < 1000 || o.Id >= 2000).ToList();
        ctx.Check("old subscriber sees the newer versions as V2 with all fields", upgraded.All(o => o is OrderV2 v2 && v2.Total > 0 && v2.Customer.Length > 0), $"({upgraded.Count(o => o is OrderV2)}/{upgraded.Count} as OrderV2)");
        ctx.Check("no duplicates", oldInbox.Duplicates == 0 && newInbox.Duplicates == 0);
    }

    /// <summary>
    ///     UseAdvancedMessagePolymorphism: a subscriber to an interface receives every implementation, a subscriber
    ///     to one implementation only that one
    /// </summary>
    public static async Task PolymorphismAsync(ScenarioContext ctx)
    {
        const int dogs = 15, cats = 10, viaInterface = 5;
        var bus = ctx.Bus(b => b.UseAdvancedMessagePolymorphism());
        var all = new Inbox<IAnimal>(ctx, a => $"{a.GetType().Name}{a.Id}");
        var onlyDogs = new Inbox<Dog>(ctx, d => d.Id.ToString());

        await using var allSubscription = await bus.Bus.PubSub.SubscribeAsync<IAnimal>(
            ctx.Name("all"),
            (animal, _) =>
            {
                if (animal.Run == ctx.Run) all.Add(animal);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );
        await using var dogSubscription = await bus.Bus.PubSub.SubscribeAsync<Dog>(
            ctx.Name("dogs"),
            (dog, _) =>
            {
                if (dog.Run == ctx.Run) onlyDogs.Add(dog);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );

        for (var i = 0; i < dogs; i++)
            await bus.Bus.PubSub.PublishAsync(new Dog { Run = ctx.Run, Id = i }, ctx.Token);
        for (var i = 0; i < cats; i++)
            await bus.Bus.PubSub.PublishAsync(new Cat { Run = ctx.Run, Id = i }, ctx.Token);
        for (var i = 0; i < viaInterface; i++)
            await bus.Bus.PubSub.PublishAsync<IAnimal>(new Dog { Run = ctx.Run, Id = 100 + i }, ctx.Token);
        ctx.CountSent(dogs + cats + viaInterface);

        var allExpected = dogs + cats + viaInterface;
        ctx.Check("interface subscriber receives every implementation", await all.WaitForAsync(allExpected, TimeSpan.FromSeconds(20), ctx.Token), $"({all.Count}/{allExpected})");
        ctx.Check("runtime types kept", all.Items.Count(a => a is Dog) == dogs + viaInterface && all.Items.Count(a => a is Cat) == cats, $"({all.Items.Count(a => a is Dog)} dogs, {all.Items.Count(a => a is Cat)} cats)");
        ctx.Check("Dog subscriber receives Dog publishes", await onlyDogs.WaitForAsync(dogs, TimeSpan.FromSeconds(20), ctx.Token), $"({onlyDogs.Count}/{dogs})");
        await Task.Delay(300, ctx.Token);
        ctx.Check("Dog subscriber gets neither cats nor interface-typed publishes", onlyDogs.Count == dogs, $"({onlyDogs.Count})");
    }
}
