namespace EasyNetQ.AspNetCore.SignalR;

/// <summary>
///     Settings of the EasyNetQ SignalR backplane. Built with <see cref="EasyNetQBackplaneConfigurator" />.
/// </summary>
public sealed record EasyNetQBackplaneOptions
{
    /// <summary>Prefix of every exchange and queue name; servers of one application must share it</summary>
    public string Prefix { get; init; } = "signalr";

    /// <summary>Unique name of this server; names its queue and addresses acks and client results to it</summary>
    public string ServerName { get; init; } = $"{Environment.MachineName}_{Guid.NewGuid():N}";

    /// <summary>How long a group change for a connection on another server waits for that server's ack</summary>
    public TimeSpan AckTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Idle time after which the broker deletes a server's (durable) queue (<c>x-expires</c>). Long enough to
    ///     survive a reconnect, short enough that a crashed server's queue does not collect messages for long.
    /// </summary>
    public TimeSpan QueueExpiry { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Messages the server's consumer may hold unacknowledged</summary>
    public ushort PrefetchCount { get; init; } = 100;
}

/// <summary>
///     Fluent configuration of the backplane: <c>AddSignalR().AddEasyNetQ(b =&gt; b.Prefix("chat").AckTimeout(...))</c>
/// </summary>
public sealed class EasyNetQBackplaneConfigurator
{
    private EasyNetQBackplaneOptions options = new();

    /// <summary>Prefix of every exchange and queue name; servers of one application must share it</summary>
    public EasyNetQBackplaneConfigurator Prefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        options = options with { Prefix = prefix };
        return this;
    }

    /// <summary>Unique name of this server (default: machine name plus a random suffix)</summary>
    public EasyNetQBackplaneConfigurator ServerName(string serverName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        options = options with { ServerName = serverName };
        return this;
    }

    /// <summary>How long a cross-server group change waits for the owning server's ack</summary>
    public EasyNetQBackplaneConfigurator AckTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        options = options with { AckTimeout = timeout };
        return this;
    }

    /// <summary>Idle time after which the broker deletes a server's queue</summary>
    public EasyNetQBackplaneConfigurator QueueExpiry(TimeSpan expiry)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expiry, TimeSpan.FromSeconds(1));
        options = options with { QueueExpiry = expiry };
        return this;
    }

    /// <summary>Messages the server's consumer may hold unacknowledged</summary>
    public EasyNetQBackplaneConfigurator PrefetchCount(ushort prefetchCount)
    {
        ArgumentOutOfRangeException.ThrowIfZero(prefetchCount);
        options = options with { PrefetchCount = prefetchCount };
        return this;
    }

    internal EasyNetQBackplaneOptions Build() => options;
}
