using System.Text;
using System.Text.Json;
using EasyNetQ.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class ErrorScenarios
{
    private const string DefaultErrorQueue = "EasyNetQ_Default_Error_Queue";

    /// <summary>
    ///     The default error queue and a named quorum error queue: what lands there (the Error envelope), what does
    ///     not, and the LogFailedMessageBodies opt-in (event 601) next to the always-on failure log (event 600)
    /// </summary>
    public static async Task ErrorsAsync(ScenarioContext ctx)
    {
        const int failing = 5, passing = 10;

        // the default error queue is shared with anything else on the broker: drain it first so the counts are ours
        if (await ctx.Admin.QueueExistsAsync(DefaultErrorQueue, ctx.Token))
            await ctx.Admin.DrainAsync(DefaultErrorQueue, ctx.Token);

        var defaultBus = ctx.Bus();
        var defaultResult = await FailSomeAsync(ctx, defaultBus, "default", failing, passing);
        ctx.Check("default: passing messages handled", defaultResult.Handled == passing, $"({defaultResult.Handled}/{passing})");
        ctx.Check("default: failures reach EasyNetQ_Default_Error_Queue", await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(DefaultErrorQueue, ctx.Token) >= failing, TimeSpan.FromSeconds(10), ctx.Token));
        var defaultErrors = await ctx.Admin.DrainAsync(DefaultErrorQueue, ctx.Token);
        CheckEnvelopes(ctx, "default", defaultBus, defaultErrors, defaultResult.Queue, failing);
        if (await ctx.Admin.QueueTypeAsync(DefaultErrorQueue, ctx.Token) is { } defaultType)
            ctx.Check("default: error queue is classic", defaultType == "classic", defaultType);

        var named = ctx.Name("errors");
        ctx.DeleteQueueLater(named);
        ctx.DeleteExchangeLater(named);
        var namedBus = ctx.Bus(b => b.UseRabbitMq(r => r.ErrorQueue(named, q => q.Quorum()).LogFailedMessageBodies()));
        var namedResult = await FailSomeAsync(ctx, namedBus, "named", failing, passing);
        ctx.Check("named: failures reach the named error queue", await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(named, ctx.Token) >= failing, TimeSpan.FromSeconds(10), ctx.Token));
        ctx.Check("named: the error exchange carries the same name", await ctx.Admin.ExchangeExistsAsync(named, ctx.Token));
        if (await ctx.Admin.QueueTypeAsync(named, ctx.Token) is { } namedType)
            ctx.Check("named: error queue is a quorum queue", namedType == "quorum", namedType);
        else
            ctx.Skip("named: error queue is a quorum queue", "no management API");
        var namedErrors = await ctx.Admin.DrainAsync(named, ctx.Token);
        CheckEnvelopes(ctx, "named", namedBus, namedErrors, namedResult.Queue, failing);
        ctx.Check("named: nothing went to the default error queue", await ctx.Admin.MessageCountAsync(DefaultErrorQueue, ctx.Token) == 0);

        var defaultLogs = defaultBus.Logs.Entries;
        var namedLogs = namedBus.Logs.Entries;
        ctx.Check("event 600 logged for each failure (both buses)", defaultLogs.Count(e => e.EventId.Id == 600) >= failing && namedLogs.Count(e => e.EventId.Id == 600) >= failing,
            $"({defaultLogs.Count(e => e.EventId.Id == 600)}, {namedLogs.Count(e => e.EventId.Id == 600)})");
        ctx.Check("failed bodies not logged by default (event 601)", defaultLogs.All(e => e.EventId.Id != 601), $"({defaultLogs.Count(e => e.EventId.Id == 601)})");
        var bodies = namedLogs.Where(e => e.EventId.Id == 601).ToList();
        ctx.Check("LogFailedMessageBodies logs each failed body (event 601, base64)", bodies.Count >= failing && bodies.All(e => DecodesToPoison(e.Message, ctx.Run)), $"({bodies.Count})");
    }

    private sealed record FailResult(string Queue, int Handled);

    private static async Task<FailResult> FailSomeAsync(ScenarioContext ctx, SoakBus bus, string name, int failing, int passing)
    {
        var handled = new Inbox<Poison>(ctx, p => p.Id.ToString());
        await using var subscription = await bus.Bus.PubSub.SubscribeAsync<Poison>(ctx.Name(name), (poison, _) =>
        {
            if (poison.Run != ctx.Run) return Task.CompletedTask;
            if (poison.Fail) throw new InvalidOperationException($"soak poison {poison.Id}");
            handled.Add(poison);
            return Task.CompletedTask;
        }, c => c.WithAutoDelete(), ctx.Token);

        for (var i = 0; i < failing + passing; i++)
            await bus.Bus.PubSub.PublishAsync(new Poison(ctx.Run, i, i < failing), $"poison.{name}", ctx.Token);
        ctx.CountSent(failing + passing);
        await handled.WaitForAsync(passing, TimeSpan.FromSeconds(10), ctx.Token);
        await Task.Delay(500, ctx.Token);
        return new FailResult(subscription.Queue.Name, handled.Count);
    }

    private static void CheckEnvelopes(ScenarioContext ctx, string name, SoakBus bus, List<PullResult> errors, string queue, int failing)
    {
        var registry = bus.Provider.GetRequiredService<IMessageTypeRegistry>();
        var conventions = bus.Provider.GetRequiredService<IConventions>();
        var poisonWireName = registry.GetOrAdd<Poison>().WireName;
        var exchange = conventions.ExchangeNamingConvention(typeof(Poison));
        var ours = new List<JsonElement>();
        var envelopeTypes = new HashSet<string?>();
        foreach (var error in errors)
        {
            using var document = JsonDocument.Parse(error.Body);
            var root = document.RootElement;
            if (root.TryGetProperty("Queue", out var q) && q.GetString() == queue)
            {
                ours.Add(root.Clone());
                envelopeTypes.Add(error.Properties.Type);
            }
        }
        ctx.Check($"{name}: one Error envelope per failure", ours.Count == failing, $"({ours.Count}/{failing})");
        var problems = new List<string>();
        foreach (var error in ours)
        {
            var message = error.GetProperty("Message").GetString() ?? "";
            if (error.GetProperty("Exchange").GetString() != exchange) problems.Add("Exchange");
            if (!(error.GetProperty("RoutingKey").GetString() ?? "").StartsWith($"poison.{name}", StringComparison.Ordinal)) problems.Add("RoutingKey");
            if (!(error.GetProperty("Exception").GetString() ?? "").Contains("soak poison", StringComparison.Ordinal)) problems.Add("Exception");
            if (!message.Contains(ctx.Run, StringComparison.Ordinal) || !message.Contains("\"Fail\":true", StringComparison.Ordinal)) problems.Add("Message");
            if (DateTime.UtcNow - error.GetProperty("DateTime").GetDateTime().ToUniversalTime() > TimeSpan.FromMinutes(5)) problems.Add("DateTime");
            var properties = error.GetProperty("BasicProperties");
            if (!properties.TryGetProperty("Type", out var type) || type.GetString() != poisonWireName) problems.Add("BasicProperties.Type");
            if (!properties.TryGetProperty("CorrelationId", out var correlationId) || string.IsNullOrEmpty(correlationId.GetString())) problems.Add("BasicProperties.CorrelationId");
        }
        ctx.Check($"{name}: Error envelope fields (Exchange, RoutingKey, Queue, Exception, Message, DateTime, BasicProperties)", problems.Count == 0, problems.Count == 0 ? "" : $"(wrong: {string.Join(",", problems.Distinct())})");
        ctx.Check($"{name}: envelope type property names the Error type", envelopeTypes.Count == 1 && envelopeTypes.Single()?.Contains("Error", StringComparison.Ordinal) == true, string.Join(",", envelopeTypes));
    }

    private static bool DecodesToPoison(string logMessage, string run)
    {
        var marker = "body=";
        var index = logMessage.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0) return false;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(logMessage[(index + marker.Length)..].Trim()));
            return json.Contains(run, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
