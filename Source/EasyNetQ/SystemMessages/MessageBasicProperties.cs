using System;
using System.Collections;
using RabbitMQ.Client;

namespace EasyNetQ.SystemMessages
{
    /// <summary>
    /// A serializable (by Json.Net) verion of IBasicProperties
    /// </summary>
    [Serializable]
    public class MessageBasicProperties
    {
        public MessageBasicProperties()
        {
            Headers = new Hashtable();
        }

        public MessageBasicProperties(IBasicProperties basicProperties) : this()
        {
            CopyFrom(basicProperties);
        }

        public void CopyFrom(IBasicProperties basicProperties)
        {
            ContentTypePresent = basicProperties.IsContentTypePresent();
            ContentEncodingPresent = basicProperties.IsContentEncodingPresent();
            HeadersPresent = basicProperties.IsHeadersPresent();
            DeliveryModePresent = basicProperties.IsDeliveryModePresent();
            PriorityPresent = basicProperties.IsPriorityPresent();
            CorrelationIdPresent = basicProperties.IsCorrelationIdPresent();
            ReplyToPresent = basicProperties.IsReplyToPresent();
            ExpirationPresent = basicProperties.IsExpirationPresent();
            MessageIdPresent = basicProperties.IsMessageIdPresent();
            TimestampPresent = basicProperties.IsTimestampPresent();
            TypePresent = basicProperties.IsTypePresent();
            UserIdPresent = basicProperties.IsUserIdPresent();
            AppIdPresent = basicProperties.IsAppIdPresent();
            ClusterIdPresent = basicProperties.IsClusterIdPresent();

            ContentType = basicProperties.ContentType;
            ContentEncoding = basicProperties.ContentEncoding;
            DeliveryMode = basicProperties.DeliveryMode;
            Priority = basicProperties.Priority;
            CorrelationId = basicProperties.CorrelationId;
            ReplyTo = basicProperties.ReplyTo;
            Expiration = basicProperties.Expiration;
            MessageId = basicProperties.MessageId;
            Timestamp = basicProperties.Timestamp.UnixTime;
            Type = basicProperties.Type;
            UserId = basicProperties.UserId;
            AppId = basicProperties.AppId;
            ClusterId = basicProperties.ClusterId;

            if (basicProperties.IsHeadersPresent())
            {
                foreach (DictionaryEntry header in basicProperties.Headers)
                {
                    Headers.Add(header.Key, header.Value);
                }
            }
        }

        public void CopyTo(IBasicProperties basicProperties)
        {
            if(ContentTypePresent)      basicProperties.ContentType      =  ContentType; 
            if(ContentEncodingPresent)  basicProperties.ContentEncoding  =  ContentEncoding; 
            if(DeliveryModePresent)     basicProperties.DeliveryMode     =  DeliveryMode; 
            if(PriorityPresent)         basicProperties.Priority         =  Priority; 
            if(CorrelationIdPresent)    basicProperties.CorrelationId    =  CorrelationId; 
            if(ReplyToPresent)          basicProperties.ReplyTo          =  ReplyTo; 
            if(ExpirationPresent)       basicProperties.Expiration       =  Expiration; 
            if(MessageIdPresent)        basicProperties.MessageId        =  MessageId; 
            if(TimestampPresent)        basicProperties.Timestamp        =  new AmqpTimestamp(Timestamp); 
            if(TypePresent)             basicProperties.Type             =  Type; 
            if(UserIdPresent)           basicProperties.UserId           =  UserId; 
            if(AppIdPresent)            basicProperties.AppId            =  AppId; 
            if(ClusterIdPresent)        basicProperties.ClusterId        =  ClusterId;

            if (HeadersPresent)
            {
                basicProperties.Headers = new Hashtable(Headers);
            }
        }

        public bool ContentTypePresent { get; set; }
        public bool ContentEncodingPresent { get; set; }
        public bool HeadersPresent { get; set; }
        public bool DeliveryModePresent { get; set; }
        public bool PriorityPresent { get; set; }
        public bool CorrelationIdPresent { get; set; }
        public bool ReplyToPresent { get; set; }
        public bool ExpirationPresent { get; set; }
        public bool MessageIdPresent { get; set; }
        public bool TimestampPresent { get; set; }
        public bool TypePresent { get; set; }
        public bool UserIdPresent { get; set; }
        public bool AppIdPresent { get; set; }
        public bool ClusterIdPresent { get; set; }

        /// <summary>
        /// MIME content type 
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// MIME content encoding 
        /// </summary>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// message header field table 
        /// </summary>
        public IDictionary Headers { get; set; }

        /// <summary>
        /// non-persistent (1) or persistent (2) 
        /// </summary>
        public byte DeliveryMode { get; set; }

        /// <summary>
        /// message priority, 0 to 9 
        /// </summary>
        public byte Priority { get; set; }

        /// <summary>
        /// application correlation identifier 
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// destination to reply to 
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// message expiration specification 
        /// </summary>
        public string Expiration { get; set; }

        /// <summary>
        /// application message identifier 
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// message timestamp 
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// message type name 
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// creating user id 
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// creating application id 
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// intra-cluster routing identifier 
        /// </summary>
        public string ClusterId { get; set; }
    }
}