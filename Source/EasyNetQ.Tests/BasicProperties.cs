using RabbitMQ.Client;

namespace EasyNetQ.Tests;

public sealed class BasicProperties : IReadOnlyBasicProperties
{
    private string contentType;
    private string contentEncoding;
    private IDictionary<string, object> headers;
    private DeliveryModes deliveryMode;
    private byte priority;
    private string correlationId;
    private string replyTo;
    private string expiration;
    private string messageId;
    private AmqpTimestamp timestamp;
    private string type;
    private string userId;
    private string appId;
    private string clusterId;

    private bool contentTypePresent;
    private bool contentEncodingPresent;
    private bool headersPresent;
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

    public string ContentType
    {
        get => contentType;
        set
        {
            contentTypePresent = value != null;
            contentType = value;
        }
    }

    public string ContentEncoding
    {
        get => contentEncoding;
        set
        {
            contentEncodingPresent = value != null;
            contentEncoding = value;
        }
    }

    public IDictionary<string, object> Headers
    {
        get => headers;
        set
        {
            headersPresent = value != null;
            headers = value;
        }
    }

    public DeliveryModes DeliveryMode
    {
        get => deliveryMode;
        set
        {
            deliveryModePresent = true;
            deliveryMode = value;
        }
    }

    public bool Persistent
    {
        get => DeliveryMode == DeliveryModes.Persistent;
        set => DeliveryMode = value ? DeliveryModes.Persistent : DeliveryModes.Transient;
    }

    public byte Priority
    {
        get => priority;
        set
        {
            priorityPresent = true;
            priority = value;
        }
    }

    public string CorrelationId
    {
        get => correlationId;
        set
        {
            correlationIdPresent = value != null;
            correlationId = value;
        }
    }

    public string ReplyTo
    {
        get => replyTo;
        set
        {
            replyToPresent = value != null;
            replyTo = value;
        }
    }

    public string Expiration
    {
        get => expiration;
        set
        {
            expirationPresent = value != null;
            expiration = value;
        }
    }

    public string MessageId
    {
        get => messageId;
        set
        {
            messageIdPresent = value != null;
            messageId = value;
        }
    }

    public AmqpTimestamp Timestamp
    {
        get => timestamp;
        set
        {
            timestampPresent = true;
            timestamp = value;
        }
    }

    public string Type
    {
        get => type;
        set
        {
            typePresent = value != null;
            type = value;
        }
    }

    public string UserId
    {
        get => userId;
        set
        {
            userIdPresent = value != null;
            userId = value;
        }
    }

    public string AppId
    {
        get => appId;
        set
        {
            appIdPresent = value != null;
            appId = value;
        }
    }

    public string ClusterId
    {
        get => clusterId;
        set
        {
            clusterIdPresent = value != null;
            clusterId = value;
        }
    }

    public void ClearContentType() => contentTypePresent = false;

    public void ClearContentEncoding() => contentEncodingPresent = false;

    public void ClearHeaders() => headersPresent = false;

    public void ClearDeliveryMode() => deliveryModePresent = false;

    public void ClearPriority() => priorityPresent = false;

    public void ClearCorrelationId() => correlationIdPresent = false;

    public void ClearReplyTo() => replyToPresent = false;

    public void ClearExpiration() => expirationPresent = false;

    public void ClearMessageId() => messageIdPresent = false;

    public void ClearTimestamp() => timestampPresent = false;

    public void ClearType() => typePresent = false;

    public void ClearUserId() => userIdPresent = false;

    public void ClearAppId() => appIdPresent = false;

    public void ClearClusterId() => clusterIdPresent = false;

    public bool IsContentTypePresent() => contentTypePresent;

    public bool IsContentEncodingPresent() => contentEncodingPresent;

    public bool IsHeadersPresent() => headersPresent;

    public bool IsDeliveryModePresent() => deliveryModePresent;

    public bool IsPriorityPresent() => priorityPresent;

    public bool IsCorrelationIdPresent() => correlationIdPresent;

    public bool IsReplyToPresent() => replyToPresent;

    public bool IsExpirationPresent() => expirationPresent;

    public bool IsMessageIdPresent() => messageIdPresent;

    public bool IsTimestampPresent() => timestampPresent;

    public bool IsTypePresent() => typePresent;

    public bool IsUserIdPresent() => userIdPresent;

    public bool IsAppIdPresent() => appIdPresent;

    public bool IsClusterIdPresent() => clusterIdPresent;

    public PublicationAddress ReplyToAddress
    {
        get => PublicationAddress.Parse(ReplyTo);
        set => ReplyTo = value.ToString();
    }

    public ushort ProtocolClassId => 60;
    public string ProtocolClassName => "basic";
}
