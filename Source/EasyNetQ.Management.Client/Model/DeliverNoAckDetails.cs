using System;
using EasyNetQ.Management.Client.Serialization;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Model
{
    public class DeliverNoAckDetails
    {
        public Double Rate { get; set; }
        public Int64 Interval { get; set; }

        [JsonConverter(typeof(UnixMsDateTimeConverter))]
        public DateTime LastEvent { get; set; }
    }
}