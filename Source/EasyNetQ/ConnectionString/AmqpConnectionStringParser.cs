using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Authentication;
using EasyNetQ.Internals;

namespace EasyNetQ.ConnectionString
{
    using UpdateConfiguration = Func<ConnectionConfiguration, Dictionary<string, string>, ConnectionConfiguration>;

    /// <inheritdoc />
    public class AmqpConnectionStringParser : IConnectionStringParser
    {
        private static readonly IReadOnlyCollection<string> SupportedSchemes = new[] {"amqp", "amqps"};

        private static readonly List<UpdateConfiguration> Parsers = new List<UpdateConfiguration>
        {
            BuildKeyValueParser("requestedHeartbeat", c => TimeSpan.FromSeconds(int.Parse(c)), c => c.RequestedHeartbeat),
            BuildKeyValueParser("prefetchCount", ushort.Parse, c => c.PrefetchCount),
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
            var host = new HostConfiguration
            {
                Host = string.IsNullOrEmpty(uri.Host) ? "localhost" : uri.Host,
                Port = uri.Port == -1
                    ? (ushort) (secured ? ConnectionConfiguration.DefaultAmqpsPort : ConnectionConfiguration.DefaultPort)
                    : (ushort) uri.Port,
            };
            if (secured)
            {
                host.Ssl.Enabled = true;
                host.Ssl.Version = SslProtocols.None;
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

                configuration.UserName = Uri.EscapeUriString(userPass[0]);
                if (userPass.Length == 2) configuration.Password = Uri.EscapeUriString(userPass[1]);
            }

            if (uri.Segments.Length > 2)
                throw new ArgumentException($"Multiple segments in path of AMQP URI: {string.Join(", ", uri.Segments)}");

            if (uri.Segments.Length == 2) configuration.VirtualHost = Uri.EscapeUriString(uri.Segments[1]);

            var query = uri.ParseQuery();
            return Parsers.Aggregate(configuration, (current, parser) => parser(current, query));
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
            Preconditions.CheckNotNull(getter, "getter");

            var memberEx = getter.Body as MemberExpression;

            Preconditions.CheckNotNull(memberEx, "getter", "Body is not a member-expression.");

            var property = memberEx.Member as PropertyInfo;

            Preconditions.CheckNotNull(property, "getter", "Member is not a property.");
            Preconditions.CheckTrue(property.CanWrite, "getter", "Member is not a writeable property.");

            return (Action<TContaining, TProperty>) property.GetSetMethod().CreateDelegate(typeof(Action<TContaining, TProperty>));
        }
    }
}
