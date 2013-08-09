namespace EasyNetQ.Management.Client.Serialization
{
    using System;
    using System.Collections.Generic;
    using Model;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    class MessageStatsOrEmptyArrayConverter : ObjectOrEmptyArrayConverter<MessageStats>
    {
        
    }
    class QueueTotalsOrEmptyArrayConverter : ObjectOrEmptyArrayConverter<List<object>>
    {
        
    }
    class ObjectOrEmptyArrayConverter<T> : JsonConverter where T:new()
    {
        // From http://stackoverflow.com/questions/17171737/how-to-deserialize-json-data-which-sometimes-is-an-empty-array-and-sometimes-a-s
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                reader.Read();
                if (reader.TokenType != JsonToken.EndArray)
                    throw new JsonReaderException("Empty array expected.");
                return default(T);
            }
            var jObject = JObject.Load(reader);

            // Create target object based on JObject
            var target = new T();

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }
    }
}