namespace EasyNetQ;

/// <summary>
///     Transport-neutral helpers for <see cref="MessageProperties" />
/// </summary>
public static class MessagePropertiesExtensions
{
    public static MessageProperties SetHeader(in this MessageProperties source, string key, object value)
    {
        var headers = source.Headers ?? new Dictionary<string, object>();
        headers[key] = value;
        return source with { Headers = headers };
    }
}
