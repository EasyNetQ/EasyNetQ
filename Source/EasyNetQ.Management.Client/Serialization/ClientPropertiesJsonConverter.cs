using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Management.Client.Serialization
{
    public class ClientPropertiesJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            return ReadClientProperties(reader);
        }

        private object ReadValue(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read())
                {
                    throw new Exception("Unexpected end");
                }
            }

            return reader.Value;
        }

        private ClientProperties ReadClientProperties(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read())
                {
                    throw new Exception("Unexpected end");
                }
            }

            IDictionary<String, Object> properties = new Dictionary<string, object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        String propertyName = ConvertParameterNameToDotNetConvention(reader.Value.ToString());
                        if (!reader.Read())
                        {
                            throw new Exception("Unexpected JsonReader End");
                        }

                        if (propertyName == "Capabilities")
                        {
                            properties[propertyName] = ReadCapabilities(reader);
                        }
                        else
                        {
                            properties[propertyName] = ReadValue(reader);
                        }

                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return new ClientProperties(properties);
                }
            }

            throw new Exception("Expected EndObject");  
        }

        private Capabilities ReadCapabilities(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read())
                {
                    throw new Exception("Unexpected end");
                }
            }

            IDictionary<String, Object> properties = new Dictionary<string, object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        String propertyName = ConvertParameterNameToDotNetConvention(reader.Value.ToString());
                        if (!reader.Read())
                        {
                            throw new Exception("Unexpected JsonReader End");
                        }
                        
                        properties[propertyName] = ReadValue(reader);                        
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return new Capabilities(properties);
                }
            }

            throw new Exception("Expected EndObject");  
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (ClientProperties);
        }

        private static string ConvertParameterNameToDotNetConvention(string str)
        {
            return String.Join("", 
                str
                .Replace('.','_') //Deal with things like 'basic.nack' --> 'basic_nack'
                .Split('_') //Deal with things like publisher_confirms --> PublisherConfirms
                .Select(CapitaliseFirstLetter));
        }

        private static String CapitaliseFirstLetter(String str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (char.IsUpper(str[0]))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return char.ToUpper(str[0]).ToString();
            }
            else
            {
                return char.ToUpper(str[0]) + str.Substring(1, str.Length - 1);
            } 
        }

    }
}
