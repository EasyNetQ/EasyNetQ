using EasyNetQ;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

using var bus = RabbitHutch.CreateBus(
    "host=localhost",
    x => x.EnableNewtonsoftJson().EnableConsoleLogger().EnableLegacyRpcConventions()
);

using var _ = await bus.Rpc.RespondAsync<Request, Response>(r => new Response(r.Id), cts.Token);

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
