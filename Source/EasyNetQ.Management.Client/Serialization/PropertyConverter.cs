using System;
using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyNetQ.Management.Client.Serialization
{
    public class PropertyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jToken = JToken.ReadFrom(reader);

            if (jToken.Type == JTokenType.Array)
            {
                return new Properties();
            }
            
            if (jToken.Type == JTokenType.Object)
            {
                var properties = new Properties();
                foreach (var property in ((JObject)jToken).Properties())
                {
                    if(property.Name == "headers")
                    {
                        if (property.Value.Type == JTokenType.Object)
                        {
                            var headers = (JObject) property.Value;
                            foreach (var header in headers.Properties())
                            {
                                properties.Headers.Add(header.Name, header.Value.ToString());
                            }
                        }
                    }
                    else
                    {
                        properties.Add(property.Name, property.Value.ToString());
                    }
                }
                return properties;
            }

            throw new JsonException(
                string.Format("Expected array or object for properties, but was {0}", jToken.Type), null);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Properties);
        }
    }
}