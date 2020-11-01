using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     Represents various properties of a message
    /// </summary>
    public class MessageProperties : ICloneable
    {
        /// <summary>
        ///     Creates empty MessageProperties
        /// </summary>
        public MessageProperties()
        {
        }

        /// <summary>
        ///     Creates MessageProperties from IBasicProperties
        /// </summary>
        public MessageProperties(IBasicProperties basicProperties)
            : this()
        {
            CopyFrom(basicProperties);
        }

        /// <summary>
        ///     Copies IBasicProperties to MessageProperties
        /// </summary>
        public void CopyFrom(IBasicProperties basicProperties)
        {
            Preconditions.CheckNotNull(basicProperties, "basicProperties");

            if (basicProperties.IsContentTypePresent())         ContentType         = basicProperties.ContentType;
            if (basicProperties.IsContentEncodingPresent())     ContentEncoding     = basicProperties.ContentEncoding;
            if (basicProperties.IsDeliveryModePresent())        DeliveryMode        = basicProperties.DeliveryMode;
            if (basicProperties.IsPriorityPresent())            Priority            = basicProperties.Priority;
            if (basicProperties.IsCorrelationIdPresent())       CorrelationId       = basicProperties.CorrelationId;
            if (basicProperties.IsReplyToPresent())             ReplyTo             = basicProperties.ReplyTo;
            if (basicProperties.IsExpirationPresent())          Expiration          = basicProperties.Expiration;
            if (basicProperties.IsMessageIdPresent())           MessageId           = basicProperties.MessageId;
            if (basicProperties.IsTimestampPresent())           Timestamp           = basicProperties.Timestamp.UnixTime;
            if (basicProperties.IsTypePresent())                Type                = basicProperties.Type;
            if (basicProperties.IsUserIdPresent())              UserId              = basicProperties.UserId;
            if (basicProperties.IsAppIdPresent())               AppId               = basicProperties.AppId;
            if (basicProperties.IsClusterIdPresent())           ClusterId           = basicProperties.ClusterId;

            if (basicProperties.IsHeadersPresent())
                Headers = basicProperties.Headers == null || basicProperties.Headers.Count == 0
                    ? null
                    : new Dictionary<string, object>(basicProperties.Headers);
        }

        /// <summary>
        ///     Copies MessageProperties to IBasicProperties
        /// </summary>
        public void CopyTo(IBasicProperties basicProperties)
        {
            Preconditions.CheckNotNull(basicProperties, "basicProperties");

            if (contentTypePresent)      basicProperties.ContentType      =  contentType;
            if (contentEncodingPresent)  basicProperties.ContentEncoding  =  contentEncoding;
            if (deliveryModePresent)     basicProperties.DeliveryMode     =  deliveryMode;
            if (priorityPresent)         basicProperties.Priority         =  priority;
            if (correlationIdPresent)    basicProperties.CorrelationId    =  correlationId;
            if (replyToPresent)          basicProperties.ReplyTo          =  replyTo;
            if (expirationPresent)       basicProperties.Expiration       =  expiration;
            if (messageIdPresent)        basicProperties.MessageId        =  messageId;
            if (timestampPresent)        basicProperties.Timestamp        =  new AmqpTimestamp(timestamp);
            if (typePresent)             basicProperties.Type             =  type;
            if (userIdPresent)           basicProperties.UserId           =  userId;
            if (appIdPresent)            basicProperties.AppId            =  appId;
            if (clusterIdPresent)        basicProperties.ClusterId        =  clusterId;

            if (headers != null && headers.Count > 0)
                basicProperties.Headers = new Dictionary<string, object>(headers);
        }

        /// <inheritdoc />
        public object Clone()
        {
            var copy = new MessageProperties();

            if (contentTypePresent)     copy.ContentType     = contentType;
            if (contentEncodingPresent) copy.ContentEncoding = contentEncoding;
            if (deliveryModePresent)    copy.DeliveryMode    = deliveryMode;
            if (priorityPresent)        copy.Priority        = priority;
            if (correlationIdPresent)   copy.CorrelationId   = correlationId;
            if (replyToPresent)         copy.ReplyTo         = replyTo;
            if (expirationPresent)      copy.Expiration      = expiration;
            if (messageIdPresent)       copy.MessageId       = messageId;
            if (timestampPresent)       copy.Timestamp       = timestamp;
            if (typePresent)            copy.Type            = type;
            if (userIdPresent)          copy.UserId          = userId;
            if (appIdPresent)           copy.AppId           = appId;
            if (clusterIdPresent)       copy.ClusterId       = clusterId;

            if (headers != null && headers.Count > 0)
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
            set { contentType = CheckShortString(value, "ContentType"); contentTypePresent = true; }
        }

        private string contentEncoding;

        /// <summary>
        ///     MIME content encoding
        /// </summary>
        public string ContentEncoding
        {
            get => contentEncoding;
            set { contentEncoding = CheckShortString(value, "ContentEncoding"); contentEncodingPresent = true; }
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
            set { correlationId = CheckShortString(value, "CorrelationId"); correlationIdPresent = true; }
        }

        private string replyTo;

        /// <summary>
        ///     Destination to reply to
        /// </summary>
        public string ReplyTo
        {
            get => replyTo;
            set { replyTo = CheckShortString(value, "ReplyTo"); replyToPresent = true; }
        }

        private string expiration;

        /// <summary>
        ///     Message expiration specification
        /// </summary>
        public string Expiration
        {
            get => expiration;
            set { expiration = CheckShortString(value, "Expiration"); expirationPresent = true; }
        }

        private string messageId;

        /// <summary>
        ///     Application message identifier
        /// </summary>
        public string MessageId
        {
            get => messageId;
            set { messageId = CheckShortString(value, "MessageId"); messageIdPresent = true; }
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
            set { type = CheckShortString(value, "Type"); typePresent = true; }
        }

        private string userId;

        /// <summary>
        ///     Creating user id
        /// </summary>
        public string UserId
        {
            get => userId;
            set { userId = CheckShortString(value, "UserId"); userIdPresent = true; }
        }

        private string appId;

        /// <summary>
        ///     Application id
        /// </summary>
        public string AppId
        {
            get => appId;
            set { appId = CheckShortString(value, "AppId"); appIdPresent = true; }
        }

        private string clusterId;

        /// <summary>
        ///     Intra-cluster routing identifier
        /// </summary>
        public string ClusterId
        {
            get => clusterId;
            set { clusterId = CheckShortString(value, "ClusterId"); clusterIdPresent = true; }
        }

        /// <summary>
        ///     True if ContentType is present
        /// </summary>
        public bool ContentTypePresent => contentTypePresent;

        /// <summary>
        ///     True if ContentEncoding is present
        /// </summary>
        public bool ContentEncodingPresent => contentEncodingPresent;

        /// <summary>
        ///     True if Headers is present
        /// </summary>
        public bool HeadersPresent => headers != null && headers.Count > 0;

        /// <summary>
        ///     True if DeliveryMode is present
        /// </summary>
        public bool DeliveryModePresent => deliveryModePresent;

        /// <summary>
        ///     True if Priority is present
        /// </summary>
        public bool PriorityPresent => priorityPresent;

        /// <summary>
        ///     True if CorrelationId is present
        /// </summary>
        public bool CorrelationIdPresent => correlationIdPresent;

        /// <summary>
        ///     True if ReplyTo is present
        /// </summary>
        public bool ReplyToPresent => replyToPresent;

        /// <summary>
        ///     True if Expiration is present
        /// </summary>
        public bool ExpirationPresent => expirationPresent;

        /// <summary>
        ///     True if MessageId is present
        /// </summary>
        public bool MessageIdPresent => messageIdPresent;

        /// <summary>
        ///     True if Timestamp is present
        /// </summary>
        public bool TimestampPresent => timestampPresent;

        /// <summary>
        ///     True if Type is present
        /// </summary>
        public bool TypePresent => typePresent;

        /// <summary>
        ///     True if UserId is present
        /// </summary>
        public bool UserIdPresent => userIdPresent;

        /// <summary>
        ///     True if AppId is present
        /// </summary>
        public bool AppIdPresent => appIdPresent;

        /// <summary>
        ///     True if ClusterId is present
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
}
