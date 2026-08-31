using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EasyNetQ.Generators.Tests;

public class MessagingModuleGeneratorTests
{
    [Fact]
    public void Should_harvest_publish_call_site()
    {
        var result = GeneratorTestHarness.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using EasyNetQ;

            namespace App;

            public class OrderPlaced { public string? Id { get; set; } }

            public class Publisher(IBus bus)
            {
                public Task PublishAsync(OrderPlaced message) => bus.PubSub.PublishAsync(message, CancellationToken.None);
            }
            """);

        result.GeneratorDiagnostics.Should().BeEmpty();
        result.CompilationErrors.Should().BeEmpty();
        result.AllGenerated.Should().Contain("registry.GetOrAdd<global::App.OrderPlaced>();");
        result.AllGenerated.Should().Contain("[assembly: global::EasyNetQ.EasyNetQModule(typeof(global::EasyNetQ.Generated.GeneratorTests.MessagingModule))]");
    }

    [Fact]
    public void Should_harvest_consumer_implementations_and_annotated_types()
    {
        var result = GeneratorTestHarness.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using EasyNetQ;
            using EasyNetQ.AutoSubscribe;

            namespace App;

            [Queue("orders")]
            public class OrderPlaced { public string? Id { get; set; } }

            public class OrderShipped { public string? Id { get; set; } }

            public class OrderConsumer : IConsumeAsync<OrderShipped>
            {
                public Task ConsumeAsync(OrderShipped message, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        result.CompilationErrors.Should().BeEmpty();
        result.AllGenerated.Should().Contain("registry.GetOrAdd<global::App.OrderPlaced>();");
        result.AllGenerated.Should().Contain("registry.GetOrAdd<global::App.OrderShipped>();");
    }

    [Fact]
    public void Should_harvest_assembly_level_opt_in()
    {
        var result = GeneratorTestHarness.Run("""
            using EasyNetQ;

            [assembly: EasyNetQMessages(typeof(App.ContractMessage))]

            namespace App;

            public class ContractMessage { public int Value { get; set; } }
            """);

        result.CompilationErrors.Should().BeEmpty();
        result.AllGenerated.Should().Contain("registry.GetOrAdd<global::App.ContractMessage>();");
    }

    [Fact]
    public void Should_intercept_AddEasyNetQ_and_register_modules()
    {
        var result = GeneratorTestHarness.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using EasyNetQ;
            using Microsoft.Extensions.DependencyInjection;

            namespace App;

            public class OrderPlaced { public string? Id { get; set; } }

            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddEasyNetQ("host=localhost");
                }

                public static Task Use(IBus bus) => bus.PubSub.PublishAsync(new OrderPlaced(), CancellationToken.None);
            }
            """);

        result.CompilationErrors.Should().BeEmpty();
        result.AllGenerated.Should().Contain("InterceptsLocation");
        result.AllGenerated.Should().Contain("GeneratedModules.AddGeneratedModules(builder)");
    }

    [Fact]
    public void Should_not_emit_without_discoveries()
    {
        var result = GeneratorTestHarness.Run("""
            namespace App;

            public class NothingToSeeHere;
            """);

        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void Should_skip_unharvestable_type_arguments()
    {
        var result = GeneratorTestHarness.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using EasyNetQ;

            namespace App;

            public class Relay(IBus bus)
            {
                // open generic: T must not be emitted
                public Task RelayAsync<T>(T message) => bus.PubSub.PublishAsync(message, CancellationToken.None);

                private class Hidden { }

                // private nested: must not be emitted
                public Task PublishHiddenAsync() => bus.PubSub.PublishAsync(new Hidden(), CancellationToken.None);
            }
            """);

        result.CompilationErrors.Should().BeEmpty();
        result.AllGenerated.Should().NotContain("GetOrAdd<T>");
        result.AllGenerated.Should().NotContain("Hidden");
    }

    [Fact]
    public void Should_compose_modules_from_referenced_assemblies()
    {
        // 1. contracts assembly, compiled with the generator, exposing its own module
        var contracts = GeneratorTestHarness.Run("""
            using EasyNetQ;

            [assembly: EasyNetQMessages(typeof(Contracts.InvoiceCreated))]

            namespace Contracts;

            public class InvoiceCreated { public decimal Amount { get; set; } }
            """, assemblyName: "Contracts");
        contracts.CompilationErrors.Should().BeEmpty();

        using var stream = new MemoryStream();
        contracts.OutputCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.Should().BeTrue();
        var contractsReference = MetadataReference.CreateFromImage(stream.ToArray());

        // 2. host assembly referencing contracts: its AddGeneratedModules must add the contracts module too
        var host = GeneratorTestHarness.Run("""
            using EasyNetQ;
            using Microsoft.Extensions.DependencyInjection;

            namespace Host;

            public static class Startup
            {
                public static void Configure(IServiceCollection services) => services.AddEasyNetQ();
            }
            """, assemblyName: "Host", extraReferences: [contractsReference]);

        host.CompilationErrors.Should().BeEmpty();
        host.AllGenerated.Should().Contain("new global::EasyNetQ.Generated.Contracts.MessagingModule()");
    }
}
