using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using System.Reflection;

namespace EasyNetQ
{
    public class MessageProperties
#if NETFX
        : ICloneable
#endif
    {
        public MessageProperties()
        {
            Headers = new Dictionary<string, object>();
        }

        public MessageProperties(IBasicProperties basicProperties)
            : this()
        {
            CopyFrom(basicProperties);
        }

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
            {
                foreach (var header in basicProperties.Headers)
                {
                    Headers.Add(header.Key, header.Value);
                }
            }
        }

        public void CopyTo(IBasicProperties basicProperties)
        {
            Preconditions.CheckNotNull(basicProperties, "basicProperties");

            if(contentTypePresent)      basicProperties.ContentType      =  ContentType; 
            if(contentEncodingPresent)  basicProperties.ContentEncoding  =  ContentEncoding; 
            if(deliveryModePresent)     basicProperties.DeliveryMode     =  DeliveryMode; 
            if(priorityPresent)         basicProperties.Priority         =  Priority; 
            if(correlationIdPresent)    basicProperties.CorrelationId    =  CorrelationId; 
            if(replyToPresent)          basicProperties.ReplyTo          =  ReplyTo; 
            if(expirationPresent)       basicProperties.Expiration       =  Expiration; 
            if(messageIdPresent)        basicProperties.MessageId        =  MessageId; 
            if(timestampPresent)        basicProperties.Timestamp        =  new AmqpTimestamp(Timestamp); 
            if(typePresent)             basicProperties.Type             =  Type; 
            if(userIdPresent)           basicProperties.UserId           =  UserId; 
            if(appIdPresent)            basicProperties.AppId            =  AppId; 
            if(clusterIdPresent)        basicProperties.ClusterId        =  ClusterId;

            if (headersPresent)
            {
                basicProperties.Headers = new Dictionary<string, object>(Headers);
            }
        }
        public object Clone()
        {
            var copy = new MessageProperties();

            if (contentTypePresent) copy.ContentType = ContentType;
            if (contentEncodingPresent) copy.ContentEncoding = ContentEncoding;
            if (deliveryModePresent) copy.DeliveryMode = DeliveryMode;
            if (priorityPresent) copy.Priority = Priority;
            if (correlationIdPresent) copy.CorrelationId = CorrelationId;
            if (replyToPresent) copy.ReplyTo = ReplyTo;
            if (expirationPresent) copy.Expiration = Expiration;
            if (messageIdPresent) copy.MessageId = MessageId;
            if (timestampPresent) copy.Timestamp = Timestamp;
            if (typePresent) copy.Type = Type;
            if (userIdPresent) copy.UserId = UserId;
            if (appIdPresent) copy.AppId = AppId;
            if (clusterIdPresent) copy.ClusterId = ClusterId;

            if (headersPresent)
            {
                copy.Headers = new Dictionary<string, object>(Headers);
            }

            return copy;
        }

        private bool contentTypePresent = false;
        private bool contentEncodingPresent = false;
        private bool headersPresent = false;
        private bool deliveryModePresent = false;
        private bool priorityPresent = false;
        private bool correlationIdPresent = false;
        private bool replyToPresent = false;
        private bool expirationPresent = false;
        private bool messageIdPresent = false;
        private bool timestampPresent = false;
        private bool typePresent = false;
        private bool userIdPresent = false;
        private bool appIdPresent = false;
        private bool clusterIdPresent = false;

        private string contentType;

        /// <summary>
        /// MIME Content type 
        /// </summary>
        public string ContentType
        {
            get { return contentType; }
            set { contentType = CheckShortString(value, "ContentType"); contentTypePresent = true; }
        }

        private string contentEncoding;

        /// <summary>
        /// MIME content encoding 
        /// </summary>
        public string ContentEncoding
        {
            get { return contentEncoding; }
            set { contentEncoding = CheckShortString(value, "ContentEncoding"); contentEncodingPresent = true; }
        }

        private IDictionary<string, object> headers;

        /// <summary>
        /// message header field table 
        /// </summary>
        public IDictionary<string, object> Headers
        {
            get { return headers; }
            set { headers = value; headersPresent = true; }
        }

        private byte deliveryMode;

        /// <summary>
        /// non-persistent (1) or persistent (2) 
        /// </summary>
        public byte DeliveryMode
        {
            get { return deliveryMode; }
            set { deliveryMode = value; deliveryModePresent = true; }
        }

        private byte priority;

        /// <summary>
        /// message priority, 0 to 9 
        /// </summary>
        public byte Priority
        {
            get { return priority; }
            set { priority = value; priorityPresent = true; }
        }

        private string correlationId;

        /// <summary>
        /// application correlation identifier 
        /// </summary>
        public string CorrelationId
        {
            get { return correlationId; }
            set { correlationId = CheckShortString(value, "CorrelationId"); correlationIdPresent = true; }
        }

        private string replyTo;

        /// <summary>
        /// destination to reply to 
        /// </summary>
        public string ReplyTo
        {
            get { return replyTo; }
            set { replyTo = CheckShortString(value, "ReplyTo"); replyToPresent = true; }
        }

        private string expiration;

        /// <summary>
        /// message expiration specification 
        /// </summary>
        public string Expiration
        {
            get { return expiration; }
            set { expiration = CheckShortString(value, "Expiration"); expirationPresent = true; }
        }

        private string messageId;

        /// <summary>
        /// application message identifier 
        /// </summary>
        public string MessageId
        {
            get { return messageId; }
            set { messageId = CheckShortString(value, "MessageId"); messageIdPresent = true; }
        }

        private long timestamp;

        /// <summary>
        /// message timestamp 
        /// </summary>
        public long Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; timestampPresent = true; }
        }

        private string type;

        /// <summary>
        /// message type name 
        /// </summary>
        public string Type
        {
            get { return type; }
            set { type = CheckShortString(value, "Type"); typePresent = true; }
        }

        private string userId;

        /// <summary>
        /// creating user id 
        /// </summary>
        public string UserId
        {
            get { return userId; }
            set { userId = CheckShortString(value, "UserId"); userIdPresent = true; }
        }

        private string appId;

        /// <summary>
        /// creating application id 
        /// </summary>
        public string AppId
        {
            get { return appId; }
            set { appId = CheckShortString(value, "AppId"); appIdPresent = true; }
        }

        private string clusterId;

        /// <summary>
        /// intra-cluster routing identifier 
        /// </summary>
        public string ClusterId
        {
            get { return clusterId; }
            set { clusterId = CheckShortString(value, "ClusterId"); clusterIdPresent = true; }
        }

        public bool ContentTypePresent
        {
            get { return contentTypePresent; }
            set { contentTypePresent = value; }
        }

        public bool ContentEncodingPresent
        {
            get { return contentEncodingPresent; }
            set { contentEncodingPresent = value; }
        }

        public bool HeadersPresent
        {
            get { return headersPresent; }
            set { headersPresent = value; }
        }

        public bool DeliveryModePresent
        {
            get { return deliveryModePresent; }
            set { deliveryModePresent = value; }
        }

        public bool PriorityPresent
        {
            get { return priorityPresent; }
            set { priorityPresent = value; }
        }

        public bool CorrelationIdPresent
        {
            get { return correlationIdPresent; }
            set { correlationIdPresent = value; }
        }

        public bool ReplyToPresent
        {
            get { return replyToPresent; }
            set { replyToPresent = value; }
        }

        public bool ExpirationPresent
        {
            get { return expirationPresent; }
            set { expirationPresent = value; }
        }

        public bool MessageIdPresent
        {
            get { return messageIdPresent; }
            set { messageIdPresent = value; }
        }

        public bool TimestampPresent
        {
            get { return timestampPresent; }
            set { timestampPresent = value; }
        }

        public bool TypePresent
        {
            get { return typePresent; }
            set { typePresent = value; }
        }

        public bool UserIdPresent
        {
            get { return userIdPresent; }
            set { userIdPresent = value; }
        }

        public bool AppIdPresent
        {
            get { return appIdPresent; }
            set { appIdPresent = value; }
        }

        public bool ClusterIdPresent
        {
            get { return clusterIdPresent; }
            set { clusterIdPresent = value; }
        }

        public override string ToString()
        {
            return GetType()
                .GetProperties()
                .Where(x => !x.Name.EndsWith("Present"))
                .Select(x => string.Format("{0}={1}", x.Name, GetValueString(x.GetValue(this, null))))
                .Intersperse(", ")
                .Aggregate(new StringBuilder(), (sb, x) => sb.Append(x))
                .ToString();
        }

        private static string GetValueString(object value)
        {
            if (value == null) return "NULL";

            var dictionary = value as IDictionary<string, object>;
            if (dictionary == null) return value.ToString();

            return dictionary
                .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                .Intersperse(", ")
                .SurroundWith("[", "]")
                .Aggregate(new StringBuilder(), (builder, element) => builder.Append(element))
                .ToString();
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