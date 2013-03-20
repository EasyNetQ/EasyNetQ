using System;
using EasyNetQ.Management.Client.Serialization;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Model
{
    public class PublishDetails
    {
        public Double Rate { get; set; }
        public Int64 Interval { get; set; }

        [JsonConverter(typeof(UnixMsDateTimeConverter))]
        public DateTime LastEvent { get; set; }
    }
}