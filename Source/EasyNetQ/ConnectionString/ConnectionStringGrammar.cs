using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EasyNetQ.Sprache;

namespace EasyNetQ.ConnectionString
{
    using UpdateConfiguration = Func<ConnectionConfiguration, ConnectionConfiguration>;

    public static class ConnectionStringGrammar
    {
        internal static readonly Parser<string> Text = Parse.CharExcept(';').Many().Text();
        internal static readonly Parser<ushort> UShortNumber = Parse.Number.Select(ushort.Parse);
        internal static readonly Parser<int> IntNumber = Parse.Number.Select(int.Parse);

        internal static readonly Parser<bool> Bool = Parse.CaseInsensitiveString("true").Or(Parse.CaseInsensitiveString("false")).Text().Select(x => x.ToLower() == "true");

        internal static readonly Parser<HostConfiguration> Host =
            from host in Parse.Char(c => c != ':' && c != ';' && c != ',', "host").Many().Text()
            from port in Parse.Char(':').Then(_ => UShortNumber).Or(Parse.Return((ushort) 0))
            select new HostConfiguration {Host = host, Port = port};

        internal static readonly Parser<IEnumerable<HostConfiguration>> Hosts = Host.ListDelimitedBy(',');

        internal static readonly Parser<Uri> AMQP = Parse.CharExcept(';').Many().Text().Where(x => Uri.TryCreate(x, UriKind.Absolute, out _)).Select(_ => new Uri(_));

        internal static readonly Parser<UpdateConfiguration> Part = new List<Parser<UpdateConfiguration>>
        {
            // add new connection string parts here
            BuildKeyValueParser("amqp", AMQP, c => c.AMQPConnectionString),
            BuildKeyValueParser("host", Hosts, c => c.Hosts),
            BuildKeyValueParser("port", UShortNumber, c => c.Port),
            BuildKeyValueParser("virtualHost", Text, c => c.VirtualHost),
            BuildKeyValueParser("requestedHeartbeat", UShortNumber, c => c.RequestedHeartbeat),
            BuildKeyValueParser("username", Text, c => c.UserName),
            BuildKeyValueParser("password", Text, c => c.Password),
            BuildKeyValueParser("prefetchCount", UShortNumber, c => c.PrefetchCount),
            BuildKeyValueParser("timeout", UShortNumber, c => c.Timeout),
            BuildKeyValueParser("publisherConfirms", Bool, c => c.PublisherConfirms),
            BuildKeyValueParser("persistentMessages", Bool, c => c.PersistentMessages),
            BuildKeyValueParser("product", Text, c => c.Product),
            BuildKeyValueParser("platform", Text, c => c.Platform),
            BuildKeyValueParser("useBackgroundThreads", Bool, c => c.UseBackgroundThreads),
            BuildKeyValueParser("dispatcherQueueSize", IntNumber, c => c.DispatcherQueueSize),
            BuildKeyValueParser("name", Text, c => c.Name)
        }.Aggregate((a, b) => a.Or(b));

        internal static readonly Parser<UpdateConfiguration> AMQPAlone =
            AMQP.Select(_ => (Func<ConnectionConfiguration, ConnectionConfiguration>) (configuration
                =>
            {
                configuration.AMQPConnectionString = _;
                return configuration;
            }));

        internal static readonly Parser<IEnumerable<UpdateConfiguration>> ConnectionStringBuilder = Part.ListDelimitedBy(';').Or(AMQPAlone.Once());

        public static IEnumerable<UpdateConfiguration> ParseConnectionString(string connectionString)
        {
            return ConnectionStringBuilder.Parse(connectionString);
        }

        private static Parser<UpdateConfiguration> BuildKeyValueParser<T>(
            string keyName,
            Parser<T> valueParser,
            Expression<Func<ConnectionConfiguration, T>> getter
        )
        {
            return from key in Parse.CaseInsensitiveString(keyName).Token()
                from separator in Parse.Char('=')
                from value in valueParser
                select (Func<ConnectionConfiguration, ConnectionConfiguration>) (c =>
                {
                    CreateSetter(getter)(c, value);
                    return c;
                });
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

#if !NETFX
            return (Action<TContaining, TProperty>) property.GetSetMethod().CreateDelegate(typeof(Action<TContaining, TProperty>));

#else
            return (Action<TContaining, TProperty>)
                Delegate.CreateDelegate(typeof(Action<TContaining, TProperty>),
                    property.GetSetMethod());
#endif
        }

        private static IEnumerable<T> Cons<T>(this T head, IEnumerable<T> rest)
        {
            yield return head;
            foreach (var item in rest)
                yield return item;
        }

        internal static Parser<IEnumerable<T>> ListDelimitedBy<T>(this Parser<T> parser, char delimiter)
        {
            return
                from head in parser
                from tail in Parse.Char(delimiter).Then(_ => parser).Many()
                select head.Cons(tail);
        }
    }
}
