using EasyNetQ.Internals;

namespace EasyNetQ.ConnectionString;

/// <inheritdoc />
public class AmqpConnectionStringParser : IConnectionStringParser
{
    /// <inheritdoc />
    public ConnectionConfiguration Parse(string connectionString)
    {
        var uri = new Uri(connectionString, UriKind.Absolute);
        if (uri.Scheme is not ("amqp" or "amqps"))
            throw new ArgumentException($"Wrong scheme in AMQP URI: {uri.Scheme}");

        var secured = uri.Scheme == "amqps";
        var host = new HostConfiguration(
            string.IsNullOrEmpty(uri.Host) ? "localhost" : uri.Host,
            uri.Port == -1
                ? (ushort)(secured ? ConnectionConfiguration.DefaultAmqpsPort : ConnectionConfiguration.DefaultPort)
                : (ushort)uri.Port
        );
        if (secured)
        {
            host.Ssl.Enabled = true;
            host.Ssl.ServerName = host.Host;
        }

        var configuration = new ConnectionConfiguration();
        configuration.Hosts.Add(host);

        var userInfo = uri.UserInfo;
        if (!string.IsNullOrEmpty(userInfo))
        {
            var userPass = userInfo.Split(':');
            if (userPass.Length > 2)
                throw new ArgumentException($"Bad user info in AMQP URI: {userInfo}");

            configuration.UserName = Uri.UnescapeDataString(userPass[0]);
            if (userPass.Length == 2) configuration.Password = Uri.UnescapeDataString(userPass[1]);
        }

        if (uri.Segments.Length > 2)
            throw new ArgumentException($"Multiple segments in path of AMQP URI: {string.Join(", ", uri.Segments)}");

        if (uri.Segments.Length == 2) configuration.VirtualHost = Uri.UnescapeDataString(uri.Segments[1]);

        var query = uri.ParseQuery();
        if (query is null) return configuration;

        // Query keys and value formats match the 8.x parser (note: no infinite mapping for 0/-1 here, unlike the
        // key=value parser - preserved for compatibility)
        if (query.TryGetValue("requestedHeartbeat", out var value)) configuration.RequestedHeartbeat = TimeSpan.FromSeconds(int.Parse(value));
        if (query.TryGetValue("prefetchCount", out value)) configuration.PrefetchCount = ushort.Parse(value);
        if (query.TryGetValue("consumerDispatcherConcurrency", out value)) configuration.ConsumerDispatcherConcurrency = ushort.Parse(value);
        if (query.TryGetValue("timeout", out value)) configuration.Timeout = TimeSpan.FromSeconds(int.Parse(value));
        if (query.TryGetValue("connectIntervalAttempt", out value)) configuration.ConnectIntervalAttempt = TimeSpan.FromSeconds(int.Parse(value));
        if (query.TryGetValue("publisherConfirms", out value)) configuration.PublisherConfirms = bool.Parse(value);
        if (query.TryGetValue("persistentMessages", out value)) configuration.PersistentMessages = bool.Parse(value);
        if (query.TryGetValue("product", out value)) configuration.Product = value;
        if (query.TryGetValue("platform", out value)) configuration.Platform = value;
        if (query.TryGetValue("name", out value)) configuration.Name = value;
        if (query.TryGetValue("mandatoryPublish", out value)) configuration.MandatoryPublish = bool.Parse(value);

        return configuration;
    }
}
