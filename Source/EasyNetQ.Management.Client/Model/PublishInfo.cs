using System;
using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class PublishInfo
    {
        public IDictionary<string, string> Properties { get; private set; }
        public string RoutingKey { get; private set; }
        public string Payload { get; private set; }
        public string PayloadEncoding { get; private set; }

        private readonly ISet<string> payloadEncodings = new HashSet<string> { "string", "base64" };

        public PublishInfo(IDictionary<string, string> properties, string routingKey, string payload, string payloadEncoding)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if (routingKey == null)
            {
                throw new ArgumentNullException("routingKey");
            }
            if (payload == null)
            {
                throw new ArgumentNullException("payload");
            }
            if (payloadEncoding == null)
            {
                throw new ArgumentNullException("payloadEncoding");
            }
            if (!payloadEncodings.Contains(payloadEncoding))
            {
                throw new ArgumentException(string.Format("payloadEncoding must be one of: '{0}'",
                    string.Join(", ", payloadEncodings)));
            }

            Properties = properties;
            RoutingKey = routingKey;
            Payload = payload;
            PayloadEncoding = payloadEncoding;
        }

        public PublishInfo(string routingKey, string payload) :
            this(new Dictionary<string, string>(), routingKey, payload, "string")
        {
        }
    }
}