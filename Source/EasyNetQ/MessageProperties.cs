using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ;

/// <summary>
///     Represents various properties of a message
/// </summary>
public class MessageProperties : ICloneable
{
    /// <inheritdoc />
    public object Clone()
    {
        var copy = new MessageProperties();

        if (contentTypePresent) copy.ContentType = contentType;
        if (contentEncodingPresent) copy.ContentEncoding = contentEncoding;
        if (deliveryModePresent) copy.DeliveryMode = deliveryMode;
        if (priorityPresent) copy.Priority = priority;
        if (correlationIdPresent) copy.CorrelationId = correlationId;
        if (replyToPresent) copy.ReplyTo = replyTo;
        if (expirationPresent) copy.Expiration = expiration;
        if (messageIdPresent) copy.MessageId = messageId;
        if (timestampPresent) copy.Timestamp = timestamp;
        if (typePresent) copy.Type = type;
        if (userIdPresent) copy.UserId = userId;
        if (appIdPresent) copy.AppId = appId;
        if (clusterIdPresent) copy.ClusterId = clusterId;

        if (headers?.Count > 0)
            copy.headers = new Dictionary<string, object>(headers);

        return copy;
    }

    private bool contentTypePresent;
    private bool contentEncodingPresent;
    private bool deliveryModePresent;
    private bool priorityPresent;
    private bool correlationIdPresent;
    private bool replyToPresent;
    private bool expirationPresent;
    private bool messageIdPresent;
    private bool timestampPresent;
    private bool typePresent;
    private bool userIdPresent;
    private bool appIdPresent;
    private bool clusterIdPresent;
    private string contentType;

    /// <summary>
    ///     MIME Content type
    /// </summary>
    public string ContentType
    {
        get => contentType;
        set { contentType = CheckShortString(value, nameof(ContentType)); contentTypePresent = true; }
    }

    private string contentEncoding;

    /// <summary>
    ///     MIME content encoding
    /// </summary>
    public string ContentEncoding
    {
        get => contentEncoding;
        set { contentEncoding = CheckShortString(value, nameof(ContentEncoding)); contentEncodingPresent = true; }
    }

    private IDictionary<string, object> headers;

    /// <summary>
    ///     Various headers
    /// </summary>
    public IDictionary<string, object> Headers
    {
        get => headers ??= new Dictionary<string, object>();
        set => headers = value;
    }

    private byte deliveryMode;

    /// <summary>
    ///     non-persistent (1) or persistent (2)
    /// </summary>
    public byte DeliveryMode
    {
        get => deliveryMode;
        set { deliveryMode = value; deliveryModePresent = true; }
    }

    private byte priority;

    /// <summary>
    ///     Message priority, 0 to 9
    /// </summary>
    public byte Priority
    {
        get => priority;
        set { priority = value; priorityPresent = true; }
    }

    private string correlationId;

    /// <summary>
    ///     Application correlation identifier
    /// </summary>
    public string CorrelationId
    {
        get => correlationId;
        set { correlationId = CheckShortString(value, nameof(CorrelationId)); correlationIdPresent = true; }
    }

    private string replyTo;

    /// <summary>
    ///     Destination to reply to
    /// </summary>
    public string ReplyTo
    {
        get => replyTo;
        set { replyTo = CheckShortString(value, nameof(ReplyTo)); replyToPresent = true; }
    }

    private TimeSpan? expiration;

    /// <summary>
    ///     Message expiration specification
    /// </summary>
    public TimeSpan? Expiration
    {
        get => expiration;
        set { expiration = value; expirationPresent = true; }
    }

    private string messageId;

    /// <summary>
    ///     Application message identifier
    /// </summary>
    public string MessageId
    {
        get => messageId;
        set { messageId = CheckShortString(value, nameof(MessageId)); messageIdPresent = true; }
    }

    private long timestamp;

    /// <summary>
    ///     Message timestamp
    /// </summary>
    public long Timestamp
    {
        get => timestamp;
        set { timestamp = value; timestampPresent = true; }
    }

    private string type;

    /// <summary>
    ///     Message type name
    /// </summary>
    public string Type
    {
        get => type;
        set { type = CheckShortString(value, nameof(Type)); typePresent = true; }
    }

    private string userId;

    /// <summary>
    ///     Creating user id
    /// </summary>
    public string UserId
    {
        get => userId;
        set { userId = CheckShortString(value, nameof(UserId)); userIdPresent = true; }
    }

    private string appId;

    /// <summary>
    ///     Application id
    /// </summary>
    public string AppId
    {
        get => appId;
        set { appId = CheckShortString(value, nameof(AppId)); appIdPresent = true; }
    }

    private string clusterId;

    /// <summary>
    ///     Intra-cluster routing identifier
    /// </summary>
    public string ClusterId
    {
        get => clusterId;
        set { clusterId = CheckShortString(value, nameof(ClusterId)); clusterIdPresent = true; }
    }

    /// <summary>
    ///     True if <see cref="ContentType"/> is present
    /// </summary>
    public bool ContentTypePresent => contentTypePresent;

    /// <summary>
    ///     True if <see cref="ContentEncoding"/> is present
    /// </summary>
    public bool ContentEncodingPresent => contentEncodingPresent;

    /// <summary>
    ///     True if <see cref="Headers"/> is present
    /// </summary>
    public bool HeadersPresent => headers?.Count > 0;

    /// <summary>
    ///     True if <see cref="DeliveryMode"/> is present
    /// </summary>
    public bool DeliveryModePresent => deliveryModePresent;

    /// <summary>
    ///     True if <see cref="Priority"/> is present
    /// </summary>
    public bool PriorityPresent => priorityPresent;

    /// <summary>
    ///     True if <see cref="CorrelationId"/> is present
    /// </summary>
    public bool CorrelationIdPresent => correlationIdPresent;

    /// <summary>
    ///     True if <see cref="ReplyTo"/> is present
    /// </summary>
    public bool ReplyToPresent => replyToPresent;

    /// <summary>
    ///     True if <see cref="Expiration"/> is present
    /// </summary>
    public bool ExpirationPresent => expirationPresent;

    /// <summary>
    ///     True if <see cref="MessageId"/> is present
    /// </summary>
    public bool MessageIdPresent => messageIdPresent;

    /// <summary>
    ///     True if <see cref="Timestamp"/> is present
    /// </summary>
    public bool TimestampPresent => timestampPresent;

    /// <summary>
    ///     True if <see cref="Type"/> is present
    /// </summary>
    public bool TypePresent => typePresent;

    /// <summary>
    ///     True if <see cref="UserId"/> is present
    /// </summary>
    public bool UserIdPresent => userIdPresent;

    /// <summary>
    ///     True if <see cref="AppId"/> is present
    /// </summary>
    public bool AppIdPresent => appIdPresent;

    /// <summary>
    ///     True if <see cref="ClusterId"/> is present
    /// </summary>
    public bool ClusterIdPresent => clusterIdPresent;

    /// <inheritdoc />
    public override string ToString()
    {
        return GetType()
            .GetProperties()
            .Where(x => !x.Name.EndsWith("Present"))
            .Select(x => $"{x.Name}={GetValueString(x.GetValue(this, null))}")
            .Intersperse(", ")
            .Aggregate(new StringBuilder(), (sb, x) => sb.Append(x))
            .ToString();
    }

    private static string GetValueString(object value)
    {
        if (value == null) return "NULL";

        return value is IDictionary<string, object> dictionary
            ? dictionary
                .Select(x => $"{x.Key}={x.Value}")
                .Intersperse(", ")
                .SurroundWith("[", "]")
                .Aggregate(new StringBuilder(), (builder, element) => builder.Append(element))
                .ToString()
            : value.ToString();
    }

    private static string CheckShortString(string input, string name)
    {
        if (input == null) return null;

        if (input.Length > 255)
        {
            throw new EasyNetQException("Exceeded maximum length of basic properties field '{0}'. Value: '{1}'", name, input);
        }

        return input;
    }
}
