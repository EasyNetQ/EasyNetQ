using System.Reflection;
using System.Runtime.Loader;
using EasyNetQ.AutoSubscribe;

namespace EasyNetQ.Generators.Tests;

public class AutoSubscriberGeneratorTests
{
    private const string Consumers = """
        using System.Threading;
        using System.Threading.Tasks;
        using EasyNetQ;
        using EasyNetQ.AutoSubscribe;

        namespace App;

        public class OrderPlaced { public string? Id { get; set; } }

        public class OrderShipped { public string? Id { get; set; } }

        [SubscriptionConfiguration(PrefetchCount = 7)]
        public class Billing : IConsumeAsync<OrderPlaced>, IConsume<OrderShipped>
        {
            [AutoSubscriberConsumer("billing")]
            [ForTopic("order.eu")]
            [ForTopic("order.us")]
            [SubscriptionConfiguration(PrefetchCount = 3, AutoDelete = true, Expires = 1000, Priority = 2)]
            public Task ConsumeAsync(OrderPlaced message, CancellationToken cancellationToken) => Task.CompletedTask;

            void IConsume<OrderShipped>.Consume(OrderShipped message, CancellationToken cancellationToken) { }
        }

        public abstract class AuditBase : IConsumeAsync<OrderShipped>
        {
            [ForTopic("shipped.#")]
            public virtual Task ConsumeAsync(OrderShipped message, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public partial class Audit : AuditBase
        {
            [AutoSubscriberConsumer(SubscriptionId = "audit")]
            public override Task ConsumeAsync(OrderShipped message, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        public partial class Audit;
        """;

    [Fact]
    public void Should_emit_a_registration_per_consumer_interface()
    {
        var result = GeneratorTestHarness.Run(Consumers);

        result.GeneratorDiagnostics.Should().BeEmpty();
        result.CompilationErrors.Should().BeEmpty();
        result.AllGenerated.Should().Contain(
            "global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumer.Async<global::App.OrderPlaced, global::App.Billing>("
            + "new global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumerAttribute(\"billing\"), "
            + "new string[] { new global::EasyNetQ.AutoSubscribe.ForTopicAttribute(\"order.eu\").Topic, new global::EasyNetQ.AutoSubscribe.ForTopicAttribute(\"order.us\").Topic }, "
            + "new global::EasyNetQ.AutoSubscribe.SubscriptionConfigurationAttribute() { PrefetchCount = 3, AutoDelete = true, Expires = 1000, Priority = 2 })");
        // explicit implementation, no method attributes: the class-level configuration applies
        result.AllGenerated.Should().Contain(
            "global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumer.Sync<global::App.OrderShipped, global::App.Billing>(null, null, "
            + "new global::EasyNetQ.AutoSubscribe.SubscriptionConfigurationAttribute() { PrefetchCount = 7 })");
        result.AllGenerated.Should().Contain("ServiceDescriptor.Singleton<global::EasyNetQ.AutoSubscribe.IAutoSubscriberConsumerSource>(AutoSubscriberConsumerSource.Instance)");
    }

    [Fact]
    public void Should_skip_abstract_consumers_and_emit_partial_classes_once()
    {
        var result = GeneratorTestHarness.Run(Consumers);

        result.AllGenerated.Should().NotContain("global::App.AuditBase>(");
        result.AllGenerated.Split("global::App.Audit>(").Length.Should().Be(2);
    }

    [Fact]
    public void Should_read_the_metadata_back_at_runtime()
    {
        var consumers = LoadGeneratedConsumers(Consumers);

        consumers.Select(c => (c.Info.ConcreteType.Name, c.Info.MessageType.Name)).Should().BeEquivalentTo(
            [("Audit", "OrderShipped"), ("Billing", "OrderPlaced"), ("Billing", "OrderShipped")]);

        var billing = consumers.Single(c => c.Info.MessageType.Name == "OrderPlaced").Info;
        billing.InterfaceType.Name.Should().Be("IConsumeAsync`1");
        billing.SubscriptionAttribute!.SubscriptionId.Should().Be("billing");
        billing.Topics.Should().Equal("order.eu", "order.us");
        billing.SubscriptionConfiguration.Should().BeEquivalentTo(new { PrefetchCount = (ushort)3, AutoDelete = true, Expires = 1000, Priority = 2 });

        // attributes of the overridden base method are inherited, as GetCustomAttributes(inherit: true) does
        var audit = consumers.Single(c => c.Info.ConcreteType.Name == "Audit").Info;
        audit.SubscriptionAttribute!.SubscriptionId.Should().Be("audit");
        audit.Topics.Should().Equal("shipped.#");
        audit.SubscriptionConfiguration.Should().BeNull();
    }

    [Fact]
    public void Should_warn_about_unreachable_consumers_only_when_AutoSubscribe_is_used()
    {
        const string privateConsumer = """
            using System.Threading;
            using System.Threading.Tasks;
            using EasyNetQ;
            using EasyNetQ.AutoSubscribe;
            using Microsoft.Extensions.DependencyInjection;

            namespace App;

            public class OrderPlaced { }

            public static class Startup
            {
                private sealed class Hidden : IConsumeAsync<OrderPlaced>
                {
                    public Task ConsumeAsync(OrderPlaced message, CancellationToken cancellationToken) => Task.CompletedTask;
                }

                public static void Configure(IServiceCollection services) => services.AddEasyNetQ("host=localhost")__AUTO__;
            }
            """;

        var without = GeneratorTestHarness.Run(privateConsumer.Replace("__AUTO__", ""));
        without.GeneratorDiagnostics.Should().BeEmpty();

        var with = GeneratorTestHarness.Run(privateConsumer.Replace("__AUTO__", ".AutoSubscribe(\"app\")"));
        with.CompilationErrors.Should().BeEmpty();
        with.GeneratorDiagnostics.Should().ContainSingle(d => d.Id == "ENQGEN001" && d.GetMessage().Contains("App.Startup.Hidden"));
        with.AllGenerated.Should().NotContain("AutoSubscriberConsumer.Async<");
    }

    private static IReadOnlyList<AutoSubscriberConsumer> LoadGeneratedConsumers(string source)
    {
        var result = GeneratorTestHarness.Run(source, assemblyName: "AutoSubscriberRuntime");
        result.CompilationErrors.Should().BeEmpty();
        using var stream = new MemoryStream();
        result.OutputCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.Should().BeTrue();
        var assembly = new AssemblyLoadContext(nameof(AutoSubscriberGeneratorTests), isCollectible: true).LoadFromStream(new MemoryStream(stream.ToArray()));
        var all = assembly.GetType("EasyNetQ.Generated.AutoSubscriberRuntime.AutoSubscriberConsumers")!
            .GetProperty("All", BindingFlags.Public | BindingFlags.Static)!;
        return (IReadOnlyList<AutoSubscriberConsumer>)all.GetValue(null)!;
    }
}
