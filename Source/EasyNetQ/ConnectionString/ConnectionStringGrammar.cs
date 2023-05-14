using System.Linq.Expressions;
using System.Reflection;
using EasyNetQ.Sprache;

namespace EasyNetQ.ConnectionString;

using UpdateConfiguration = Func<ConnectionConfiguration, ConnectionConfiguration>;

internal static class ConnectionStringGrammar
{
    internal static readonly Parser<string> Text = Parse.CharExcept(';').Many().Text();
    internal static readonly Parser<ushort> UShortNumber = Parse.NonNegativeNumber.Select(ushort.Parse);
    internal static readonly Parser<int?> NullableIntNumber = Parse.NonNegativeNumber.Select(x => (int?)int.Parse(x));
    internal static readonly Parser<string> MinusOne = Parse.String("-1").Text();

    internal static readonly Parser<TimeSpan> TimeSpanSeconds = Parse.NonNegativeNumber.Or(MinusOne)
        .Select(int.Parse)
        .Select(
            x => x is 0 or -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(x)
        );

    internal static readonly Parser<bool> Bool = Parse.CaseInsensitiveString("true").Or(Parse.CaseInsensitiveString("false")).Text()
        .Select(x => x.ToLower() == "true");

    internal static readonly Parser<HostConfiguration> Host =
        from host in Parse.Char(c => c != ':' && c != ';' && c != ',', "host").Many().Text()
        from port in Parse.Char(':').Then(_ => UShortNumber).Or(Parse.Return((ushort)0))
        select new HostConfiguration(host, port);

    internal static readonly Parser<IList<HostConfiguration>> Hosts = Host.ListDelimitedBy(',').Select(hosts => hosts.ToList());

    internal static readonly Parser<UpdateConfiguration> Part = new List<Parser<UpdateConfiguration>>
    {
        // add new connection string parts here
        BuildKeyValueParser("host", Hosts, c => c.Hosts),
        BuildKeyValueParser("port", UShortNumber, c => c.Port),
        BuildKeyValueParser("virtualHost", Text, c => c.VirtualHost),
        BuildKeyValueParser("requestedHeartbeat", TimeSpanSeconds, c => c.RequestedHeartbeat),
        BuildKeyValueParser("username", Text, c => c.UserName),
        BuildKeyValueParser("password", Text, c => c.Password),
        BuildKeyValueParser("prefetchCount", UShortNumber, c => c.PrefetchCount),
        BuildKeyValueParser("consumerDispatcherConcurrency", NullableIntNumber, c => c.ConsumerDispatcherConcurrency),
        BuildKeyValueParser("timeout", TimeSpanSeconds, c => c.Timeout),
        BuildKeyValueParser("connectIntervalAttempt", TimeSpanSeconds, c => c.ConnectIntervalAttempt),
        BuildKeyValueParser("publisherConfirms", Bool, c => c.PublisherConfirms),
        BuildKeyValueParser("persistentMessages", Bool, c => c.PersistentMessages),
        BuildKeyValueParser("product", Text, c => c.Product),
        BuildKeyValueParser("platform", Text, c => c.Platform),
        BuildKeyValueParser("name", Text, c => c.Name),
        BuildKeyValueParser("mandatoryPublish", Bool, c => c.MandatoryPublish),
        BuildKeyValueParser("ssl", Bool, c => c.Ssl.Enabled)
    }.Aggregate((a, b) => a.Or(b));

    internal static readonly Parser<IEnumerable<UpdateConfiguration>> ConnectionStringBuilder = Part.ListDelimitedBy(';');

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
               select (Func<ConnectionConfiguration, ConnectionConfiguration>)(c =>
               {
                   CreateSetter(getter)(c, value);
                   return c;
               });
    }

    private static Action<ConnectionConfiguration, T> CreateSetter<T>(
        Expression<Func<ConnectionConfiguration, T>> getter
    ) => CreateSetter<ConnectionConfiguration, T>(getter);

    private static Action<TContaining, TProperty> CreateSetter<TContaining, TProperty>(Expression<Func<TContaining, TProperty>> getter)
    {
        if (getter.Body is not MemberExpression memberExpr)
            throw new ArgumentOutOfRangeException(nameof(getter), "Body is not a member-expression");
        if (memberExpr.Member is not PropertyInfo propertyInfo)
            throw new ArgumentOutOfRangeException(nameof(getter), "Member is not a property");
        if (!propertyInfo.CanWrite)
            throw new ArgumentOutOfRangeException(nameof(getter), "Property is not writeable");

        var valueParameterExpr = Expression.Parameter(typeof(TProperty), "value");
        var setter = propertyInfo.GetSetMethod() ?? throw new ArgumentOutOfRangeException(nameof(getter), "No set method");
        var expr = Expression.Call(memberExpr.Expression, setter, valueParameterExpr);
        return Expression.Lambda<Action<TContaining, TProperty>>(expr, getter.Parameters.Single(), valueParameterExpr).Compile();
    }

    private static IEnumerable<T> Cons<T>(this T head, IEnumerable<T> rest)
    {
        yield return head;
        foreach (var item in rest)
            yield return item;
    }

    private static Parser<IEnumerable<T>> ListDelimitedBy<T>(this Parser<T> parser, char delimiter)
    {
        return
            from head in parser
            from tail in Parse.Char(delimiter).Then(_ => parser).Many()
            select head.Cons(tail);
    }
}
