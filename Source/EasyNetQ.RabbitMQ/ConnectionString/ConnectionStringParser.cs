namespace EasyNetQ.ConnectionString;

/// <inheritdoc />
public class ConnectionStringParser : IConnectionStringParser
{
    /// <inheritdoc />
    public ConnectionConfiguration Parse(string connectionString)
    {
        var configuration = new ConnectionConfiguration();
        var parsedAny = false;

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
                throw new EasyNetQException("Connection String {0}", $"Parsing failure: expected key=value but found '{segment}'");

            var key = segment.AsSpan(0, separatorIndex).Trim();
            var value = segment[(separatorIndex + 1)..];
            ApplyPart(configuration, key, value);
            parsedAny = true;
        }

        if (!parsedAny)
            throw new EasyNetQException("Connection String {0}", "Parsing failure: the connection string is empty");

        return configuration;
    }

    private static void ApplyPart(ConnectionConfiguration configuration, ReadOnlySpan<char> key, string value)
    {
        // Keys are case-insensitive, matching the 8.x grammar
        if (Is(key, "host")) configuration.Hosts = ParseHosts(value);
        else if (Is(key, "port")) configuration.Port = ParseUShort(key, value);
        else if (Is(key, "virtualHost")) configuration.VirtualHost = value;
        else if (Is(key, "requestedHeartbeat")) configuration.RequestedHeartbeat = ParseTimeSpanSeconds(key, value);
        else if (Is(key, "username")) configuration.UserName = value;
        else if (Is(key, "password")) configuration.Password = value;
        else if (Is(key, "prefetchCount")) configuration.PrefetchCount = ParseUShort(key, value);
        else if (Is(key, "consumerDispatcherConcurrency")) configuration.ConsumerDispatcherConcurrency = ParseUShort(key, value);
        else if (Is(key, "timeout")) configuration.Timeout = ParseTimeSpanSeconds(key, value);
        else if (Is(key, "connectIntervalAttempt")) configuration.ConnectIntervalAttempt = ParseTimeSpanSeconds(key, value);
        else if (Is(key, "publisherConfirms")) configuration.PublisherConfirms = ParseBool(key, value);
        else if (Is(key, "persistentMessages")) configuration.PersistentMessages = ParseBool(key, value);
        else if (Is(key, "product")) configuration.Product = value;
        else if (Is(key, "platform")) configuration.Platform = value;
        else if (Is(key, "name")) configuration.Name = value;
        else if (Is(key, "mandatoryPublish")) configuration.MandatoryPublish = ParseBool(key, value);
        else if (Is(key, "ssl")) configuration.Ssl.Enabled = ParseBool(key, value);
        else throw new EasyNetQException("Connection String {0}", $"Parsing failure: unknown key '{key.ToString()}'");
    }

    private static bool Is(ReadOnlySpan<char> key, string name) => key.Equals(name, StringComparison.OrdinalIgnoreCase);

    private static List<HostConfiguration> ParseHosts(string value)
    {
        var hosts = new List<HostConfiguration>();
        foreach (var hostAndPort in value.Split(','))
        {
            var separatorIndex = hostAndPort.IndexOf(':');
            hosts.Add(separatorIndex < 0
                ? new HostConfiguration(hostAndPort, 0)
                : new HostConfiguration(hostAndPort[..separatorIndex], ParseUShort("host", hostAndPort[(separatorIndex + 1)..])));
        }

        return hosts;
    }

    private static ushort ParseUShort(ReadOnlySpan<char> key, string value)
        => ushort.TryParse(value, out var result)
            ? result
            : throw new EasyNetQException("Connection String {0}", $"Parsing failure: '{value}' is not a valid non-negative number for '{key.ToString()}'");

    private static bool ParseBool(ReadOnlySpan<char> key, string value)
        => bool.TryParse(value, out var result)
            ? result
            : throw new EasyNetQException("Connection String {0}", $"Parsing failure: '{value}' is not a valid boolean for '{key.ToString()}'");

    private static TimeSpan ParseTimeSpanSeconds(ReadOnlySpan<char> key, string value)
    {
        if (!int.TryParse(value, out var seconds) || seconds < -1)
            throw new EasyNetQException("Connection String {0}", $"Parsing failure: '{value}' is not a valid number of seconds for '{key.ToString()}'");

        // 0 and -1 mean infinite, matching the 8.x grammar
        return seconds is 0 or -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(seconds);
    }
}
