using System;
using System.Collections.Generic;
using System.Dynamic;
using EasyNetQ.Management.Client.Dynamic;
using EasyNetQ.Management.Client.Serialization;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Model
{
        [JsonConverter(typeof(ClientPropertiesJsonConverter))]
        public class ClientProperties : PropertyExpando
        {
            public ClientProperties(IDictionary<String,Object> properties) : base (properties) {}

            public Capabilities Capabilities { get { return GetPropertyOrDefault<Capabilities>("Capabilities"); } }
            public string User { get { return GetPropertyOrDefault<String>("User"); } }
            public string Application { get { return GetPropertyOrDefault<String>("Application"); } }
            public string ClientApi { get { return GetPropertyOrDefault<String>("ClientApi"); } }
            public string ApplicationLocation { get { return GetPropertyOrDefault<String>("ApplicationLocation"); } }
            public DateTime Connected { get { return GetPropertyOrDefault<DateTime>("Connected"); } }
            public string EasynetqVersion { get { return GetPropertyOrDefault<String>("EasynetqVersion"); } }
            public string MachineName { get { return GetPropertyOrDefault<String>("MachineName"); } }            
            public IDictionary<String, Object> PropertiesDictionary { get { return Properties; } }
        }
}