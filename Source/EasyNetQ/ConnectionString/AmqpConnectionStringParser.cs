using System.Linq.Expressions;
using System.Reflection;
using EasyNetQ.Internals;

namespace EasyNetQ.ConnectionString;

using UpdateConfiguration = Func<ConnectionConfiguration, Dictionary<string, string>, ConnectionConfiguration>;

/// <inheritdoc />
public class AmqpConnectionStringParser : IConnectionStringParser
{
    private static readonly IReadOnlyCollection<string> SupportedSchemes = new[] { "amqp", "amqps" };

    private static readonly List<UpdateConfiguration> Parsers = new()
    {
        BuildKeyValueParser("requestedHeartbeat", c => TimeSpan.FromSeconds(int.Parse(c)), c => c.RequestedHeartbeat),
        BuildKeyValueParser("prefetchCount", ushort.Parse, c => c.PrefetchCount),
        BuildKeyValueParser("consumerDispatcherConcurrency", x => int.Parse(x), c => c.ConsumerDispatcherConcurrency),
        BuildKeyValueParser("timeout", c => TimeSpan.FromSeconds(int.Parse(c)), c => c.Timeout),
        BuildKeyValueParser("connectIntervalAttempt", c => TimeSpan.FromSeconds(int.Parse(c)), c => c.ConnectIntervalAttempt),
        BuildKeyValueParser("publisherConfirms", bool.Parse, c => c.PublisherConfirms),
        BuildKeyValueParser("persistentMessages", bool.Parse, c => c.PersistentMessages),
        BuildKeyValueParser("product", c => c, c => c.Product),
        BuildKeyValueParser("platform", c => c, c => c.Platform),
        BuildKeyValueParser("name", c => c, c => c.Name),
        BuildKeyValueParser("mandatoryPublish", bool.Parse, c => c.MandatoryPublish)
    };

    /// <inheritdoc />
    public ConnectionConfiguration Parse(string connectionString)
    {
        var uri = new Uri(connectionString, UriKind.Absolute);
        if (!SupportedSchemes.Contains(uri.Scheme))
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
        return query == null ? configuration : Parsers.Aggregate(configuration, (current, parser) => parser(current, query));
    }

    private static UpdateConfiguration BuildKeyValueParser<T>(
        string keyName,
        Func<string, T> valueParser,
        Expression<Func<ConnectionConfiguration, T>> getter
    )
    {
        return (configuration, keyValues) =>
        {
            if (keyValues != null && keyValues.TryGetValue(keyName, out var keyValue))
            {
                var parsedValue = valueParser(keyValue);
                CreateSetter(getter)(configuration, parsedValue);
            }

            return configuration;
        };
    }

    private static Action<ConnectionConfiguration, T> CreateSetter<T>(Expression<Func<ConnectionConfiguration, T>> getter)
    {
        return CreateSetter<ConnectionConfiguration, T>(getter);
    }

    /// <summary>
    /// Stolen from SO:
    /// http://stackoverflow.com/questions/4596453/create-an-actiont-to-set-a-property-when-i-am-provided-with-the-linq-expres
    /// </summary>
    /// <typeparam name="TContaining"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="getter"></param>
    /// <returns></returns>
    private static Action<TContaining, TProperty> CreateSetter<TContaining, TProperty>(Expression<Func<TContaining, TProperty>> getter)
    {
        if (getter.Body is not MemberExpression memberEx) throw new ArgumentOutOfRangeException(nameof(getter), "Body is not a member-expression.");
        if (memberEx.Member is not PropertyInfo propertyInfo) throw new ArgumentOutOfRangeException(nameof(getter), "Member is not a property.");
        if (!propertyInfo.CanWrite) throw new ArgumentOutOfRangeException(nameof(getter), "Member is not a writeable property.");

        var setMethodInfo = propertyInfo.GetSetMethod() ?? throw new ArgumentOutOfRangeException(nameof(getter), "No set method.");
        return (Action<TContaining, TProperty>)setMethodInfo.CreateDelegate(typeof(Action<TContaining, TProperty>));
    }
}
