using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EasyNetQ.Hosting;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Examples.Soak;

public sealed record SoakOptions(
    string Connection,
    string? DelayedConnection,
    Uri? Management,
    bool Verbose
);

public sealed record Scenario(string Name, bool AotSafe, string Covers, Func<ScenarioContext, Task> RunAsync);

public sealed record CheckResult(string Name, bool? Ok, string Detail);

/// <summary>
///     One run of one scenario: unique names, checks, sent/received counters, and the topology to clean up
/// </summary>
public sealed class ScenarioContext : IAsyncDisposable
{
    private readonly List<CheckResult> checks = new();
    private readonly List<string> queuesToDelete = new();
    private readonly List<string> exchangesToDelete = new();
    private readonly List<SoakBus> buses = new();
    private long sent;
    private long received;

    public ScenarioContext(Scenario scenario, SoakOptions options, AdminClient admin, CancellationToken token)
    {
        Scenario = scenario;
        Options = options;
        Admin = admin;
        Token = token;
        Run = Guid.NewGuid().ToString("N")[..8];
    }

    public Scenario Scenario { get; }
    public SoakOptions Options { get; }
    public AdminClient Admin { get; }
    public CancellationToken Token { get; }
    public string Run { get; }
    public IReadOnlyList<CheckResult> Checks => checks;
    public long Sent => Interlocked.Read(ref sent);
    public long Received => Interlocked.Read(ref received);

    public string Name(string suffix) => $"soak.{Run}.{Scenario.Name}.{suffix}";

    public void CountSent(long count = 1) => Interlocked.Add(ref sent, count);

    public void CountReceived(long count = 1) => Interlocked.Add(ref received, count);

    public void Check(string name, bool ok, string detail = "")
    {
        lock (checks) checks.Add(new CheckResult(name, ok, detail));
    }

    public void Skip(string name, string reason)
    {
        lock (checks) checks.Add(new CheckResult(name, null, reason));
    }

    public void DeleteQueueLater(string queue)
    {
        lock (queuesToDelete) queuesToDelete.Add(queue);
    }

    public void DeleteExchangeLater(string exchange)
    {
        lock (exchangesToDelete) exchangesToDelete.Add(exchange);
    }

    /// <summary>
    ///     A bus in its own container; disposed (hosted consumers stopped first) at the end of the scenario
    /// </summary>
    public SoakBus Bus(Action<IEasyNetQBuilder>? configure = null, string? connection = null, Action<IServiceCollection>? services = null)
    {
        var bus = SoakBus.Create(this, connection ?? Options.Connection, configure, services);
        buses.Add(bus);
        return bus;
    }

    public async ValueTask DisposeAsync()
    {
        for (var i = buses.Count - 1; i >= 0; i--)
        {
            try
            {
                await buses[i].DisposeAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"WARN [{Scenario.Name}] disposing a bus failed: {exception.GetType().Name}: {exception.Message}");
            }
        }

        foreach (var queue in queuesToDelete.Distinct())
            await Admin.TryDeleteQueueAsync(queue);
        foreach (var exchange in exchangesToDelete.Distinct())
            await Admin.TryDeleteExchangeAsync(exchange);
    }
}

/// <summary>
///     A container with one EasyNetQ bus, the fluent consumer host, and the captured log
/// </summary>
public sealed class SoakBus : IAsyncDisposable
{
    private readonly List<IHostedService> startedHosts = new();
    private bool disposed;

    private SoakBus(ServiceProvider provider, LogCapture logs)
    {
        Provider = provider;
        Logs = logs;
    }

    public ServiceProvider Provider { get; }
    public LogCapture Logs { get; }
    public IBus Bus => Provider.GetRequiredService<IBus>();
    public IAdvancedBus Advanced => Bus.Advanced;
    public IMessagePublisher Publisher => Provider.GetRequiredService<IMessagePublisher>();

    public static SoakBus Create(
        ScenarioContext context, string connection, Action<IEasyNetQBuilder>? configure, Action<IServiceCollection>? configureServices
    )
    {
        var logs = new LogCapture(context.Scenario.Name, context.Options.Verbose);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(logs));
        var builder = services.AddEasyNetQ(connection);
        configure?.Invoke(builder);
        configureServices?.Invoke(services);
        return new SoakBus(services.BuildServiceProvider(), logs);
    }

    /// <summary>
    ///     Starts the fluent consumers and waits until every one of them consumes
    /// </summary>
    public async Task StartConsumersAsync(CancellationToken cancellationToken)
    {
        foreach (var host in Provider.GetServices<IHostedService>())
        {
            await host.StartAsync(cancellationToken);
            startedHosts.Add(host);
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await Provider.GetRequiredService<IConsumerHostStatus>().WaitForStartedAsync(timeout.Token);
    }

    public async Task StopConsumersAsync()
    {
        foreach (var host in startedHosts)
            await host.StopAsync(CancellationToken.None);
        startedHosts.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        await StopConsumersAsync();
        await Provider.DisposeAsync();
    }
}

/// <summary>
///     A long-lived bus for topology housekeeping and broker-side assertions (queue depth, passive declares,
///     pulling from error queues), plus the optional management API for queue arguments
/// </summary>
public sealed class AdminClient : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly HttpClient? http;

    public AdminClient(SoakOptions options)
    {
        var services = new ServiceCollection();
        services.AddEasyNetQ(options.Connection);
        provider = services.BuildServiceProvider();
        if (options.Management is { } management)
        {
            http = new HttpClient { BaseAddress = new Uri(management.GetLeftPart(UriPartial.Authority)) };
            if (!string.IsNullOrEmpty(management.UserInfo))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(management.UserInfo)))
                );
        }
    }

    public IAdvancedBus Advanced => provider.GetRequiredService<IBus>().Advanced;

    public bool HasManagement => http is not null;

    /// <summary>
    ///     Ready messages in <paramref name="queue" />; 0 while it does not exist yet
    /// </summary>
    public async Task<ulong> MessageCountAsync(string queue, CancellationToken cancellationToken)
    {
        try
        {
            return (await Advanced.GetQueueStatsAsync(queue, cancellationToken)).MessagesCount;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return 0;
        }
    }

    public async Task<bool> QueueExistsAsync(string queue, CancellationToken cancellationToken)
    {
        try
        {
            await Advanced.QueueDeclarePassiveAsync(queue, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<bool> ExchangeExistsAsync(string exchange, CancellationToken cancellationToken)
    {
        try
        {
            await Advanced.ExchangeDeclarePassiveAsync(exchange, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    ///     Pulls (and acks) every message currently in <paramref name="queue" />
    /// </summary>
    public async Task<List<PullResult>> DrainAsync(string queue, CancellationToken cancellationToken, int max = 10_000)
    {
        var results = new List<PullResult>();
        await using var consumer = Advanced.CreatePullingConsumer(new Queue(queue), autoAck: true);
        while (results.Count < max)
        {
            var result = await consumer.PullAsync(cancellationToken);
            if (!result.IsAvailable) break;
            results.Add(result);
        }
        return results;
    }

    /// <summary>
    ///     The queue as the management API reports it, or null without a management endpoint
    /// </summary>
    public async Task<JsonElement?> GetQueueAsync(string queue, CancellationToken cancellationToken)
    {
        if (http is null) return null;
        using var response = await http.GetAsync($"/api/queues/%2F/{Uri.EscapeDataString(queue)}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    public async Task<string?> QueueTypeAsync(string queue, CancellationToken cancellationToken)
        => await GetQueueAsync(queue, cancellationToken) is { } json && json.TryGetProperty("type", out var type) ? type.GetString() : null;

    public async Task TryDeleteQueueAsync(string queue)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Advanced.QueueDeleteAsync(queue, cancellationToken: cts.Token);
        }
        catch
        {
            // best effort: it may be gone already (auto-delete, expiry)
        }
    }

    public async Task TryDeleteExchangeAsync(string exchange)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Advanced.ExchangeDeleteAsync(exchange, cancellationToken: cts.Token);
        }
        catch
        {
            // best effort
        }
    }

    public async ValueTask DisposeAsync()
    {
        http?.Dispose();
        await provider.DisposeAsync();
    }
}

public sealed record LogEntry(string Category, LogLevel Level, EventId EventId, string Message, Exception? Exception);

/// <summary>
///     Captures every log entry of one bus, for assertions on what EasyNetQ logs
/// </summary>
public sealed class LogCapture : ILoggerProvider
{
    private readonly string scenario;
    private readonly bool verbose;
    private readonly ConcurrentQueue<LogEntry> entries = new();

    public LogCapture(string scenario, bool verbose)
    {
        this.scenario = scenario;
        this.verbose = verbose;
    }

    public IReadOnlyCollection<LogEntry> Entries => entries;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(LogCapture owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            owner.entries.Enqueue(new LogEntry(category, logLevel, eventId, message, exception));
            if (owner.verbose && logLevel >= LogLevel.Warning)
                Console.WriteLine($"  log [{owner.scenario}] {logLevel} {category}[{eventId.Id}] {message} {exception?.GetType().Name}");
        }
    }
}

/// <summary>
///     Received-message bookkeeping: unique ids, duplicates, arrival order, and a waiter for an expected count
/// </summary>
public sealed class Inbox<T>
{
    private readonly ConcurrentDictionary<string, int> seen = new();
    private readonly ConcurrentQueue<T> items = new();
    private readonly ScenarioContext context;
    private readonly Func<T, string> key;
    private int duplicates;

    public Inbox(ScenarioContext context, Func<T, string> key)
    {
        this.context = context;
        this.key = key;
    }

    public int Count => seen.Count;
    public int Duplicates => Volatile.Read(ref duplicates);
    public IReadOnlyList<T> Items => items.ToArray();

    public void Add(T item)
    {
        context.CountReceived();
        items.Enqueue(item);
        if (seen.AddOrUpdate(key(item), 1, (_, count) => count + 1) > 1)
            Interlocked.Increment(ref duplicates);
    }

    public Task<bool> WaitForAsync(int expected, TimeSpan timeout, CancellationToken cancellationToken)
        => Soak.WaitUntilAsync(() => Count >= expected, timeout, cancellationToken);
}

public static class Soak
{
    public static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) return false;
            await Task.Delay(25, cancellationToken);
        }
        return true;
    }

    public static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!await condition())
        {
            if (DateTime.UtcNow > deadline) return false;
            await Task.Delay(100, cancellationToken);
        }
        return true;
    }

    /// <summary>
    ///     Runs <paramref name="action" /> and returns the exception it threw, or null
    /// </summary>
    public static async Task<Exception?> CatchAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static string Describe(Exception? exception)
        => exception is null ? "no exception" : $"{exception.GetType().Name}: {exception.Message.Split('\n')[0]}";
}
