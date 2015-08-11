using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sprache;

namespace EasyNetQ.ConnectionString
{
    using UpdateConfiguration = Func<ConnectionConfiguration, ConnectionConfiguration>;

    public static class ConnectionStringGrammar
    {
        public static Parser<string> Text = ConfigurationStringSharedGrammar.Text;
        public static Parser<ushort> Number = ConfigurationStringSharedGrammar.Number;
        public static Parser<bool> Bool = ConfigurationStringSharedGrammar.Bool;

        public static Parser<HostConfiguration> Host =
            from host in Parse.Char(c => c != ':' && c != ';' && c != ',', "host").Many().Text()
            from port in Parse.Char(':').Then(_ => Number).Or(Parse.Return((ushort)0))
            select new HostConfiguration { Host = host, Port = port };

        public static Parser<IEnumerable<HostConfiguration>> Hosts = Host.ListDelimitedBy(',');

        private static Uri result;
        public static Parser<Uri> AMQP = Parse.CharExcept(';').Many().Text().Where(x => Uri.TryCreate(x, UriKind.Absolute, out result)).Select(_ => new Uri(_));

        public static Parser<UpdateConfiguration> Part = new List<Parser<UpdateConfiguration>>
        {
            // add new connection string parts here
            BuildKeyValueParser("amqp", AMQP, c => c.AMQPConnectionString),
            BuildKeyValueParser("host", Hosts, c => c.Hosts),
            BuildKeyValueParser("port", Number, c => c.Port),
            BuildKeyValueParser("virtualHost", Text, c => c.VirtualHost),
            BuildKeyValueParser("requestedHeartbeat", Number, c => c.RequestedHeartbeat),
            BuildKeyValueParser("username", Text, c => c.UserName),
            BuildKeyValueParser("password", Text, c => c.Password),
            BuildKeyValueParser("prefetchcount", Number, c => c.PrefetchCount),
            BuildKeyValueParser("timeout", Number, c => c.Timeout),
            BuildKeyValueParser("publisherConfirms", Bool, c => c.PublisherConfirms),
            BuildKeyValueParser("persistentMessages", Bool, c => c.PersistentMessages),
            BuildKeyValueParser("cancelOnHaFailover", Bool, c => c.CancelOnHaFailover),
            BuildKeyValueParser("product", Text, c => c.Product),
            BuildKeyValueParser("platform", Text, c => c.Platform)
        }.Aggregate((a, b) => a.Or(b));

        public static Parser<UpdateConfiguration> AMQPAlone =
            AMQP.Select(_ => (Func<ConnectionConfiguration, ConnectionConfiguration>)(configuration
                                                                                       =>
                {
                    configuration.AMQPConnectionString = _;
                    return configuration;
                }));

        public static Parser<IEnumerable<UpdateConfiguration>> ConnectionStringBuilder = Part.ListDelimitedBy(';').Or(AMQPAlone.Once());

        public static Parser<UpdateConfiguration> BuildKeyValueParser<T>(
           string keyName,
           Parser<T> valueParser,
           Expression<Func<ConnectionConfiguration, T>> getter)
        {
            return ConfigurationStringSharedGrammar.BuildKeyValueParser(keyName, valueParser, getter);
        }
    }
}