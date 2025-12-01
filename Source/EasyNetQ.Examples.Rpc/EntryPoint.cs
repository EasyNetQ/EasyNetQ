using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(builder => builder.AddConsole());
serviceCollection.AddEasyNetQ("host=localhost")
    .UseNewtonsoftJson()
    .UseLegacyConventions();

using var provider = serviceCollection.BuildServiceProvider();

var bus = provider.GetRequiredService<IBus>();

await using var _ = await bus.Rpc.RespondAsync<Request, Response>(r => new Response(r.Id), cts.Token);

while (!cts.IsCancellationRequested)
{
    try
    {
        await bus.Rpc.RequestAsync<Request, Response>(new Request(Guid.NewGuid()), cts.Token);
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception)
    {
        await Task.Delay(5000, cts.Token);
    }
}

public record Request(Guid Id);

public record Response(Guid Id);
