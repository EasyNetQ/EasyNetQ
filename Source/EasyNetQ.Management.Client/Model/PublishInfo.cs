using System;
using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class PublishInfo
    {
        public IDictionary<string, string> properties { get; private set; }
        public string routing_key { get; private set; }
        public string payload { get; private set; }
        public string payload_encoding { get; private set; }

        public PublishInfo(IDictionary<string, string> properties, string routingKey, string payload, string payloadEncoding)
        {
            if(properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if(routingKey == null)
            {
                throw new ArgumentNullException("routingKey");
            }
            if(payload == null)
            {
                throw new ArgumentNullException("payload");
            }
            if(payloadEncoding == null)
            {
                throw new ArgumentNullException("payloadEncoding");
            }

            this.properties = properties;
            routing_key = routingKey;
            this.payload = payload;
            payload_encoding = payloadEncoding;
        }

        public PublishInfo(string routingKey, string payload) : 
            this(new Dictionary<string, string>(), routingKey, payload, "string")
        {
        }
    }
}