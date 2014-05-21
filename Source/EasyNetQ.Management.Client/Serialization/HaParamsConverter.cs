namespace EasyNetQ.Management.Client.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using Newtonsoft.Json;

    public class HaParamsConverter : JsonConverter
    {
        // Support serializing/deserializing ha-params according to http://www.rabbitmq.com/ha.html#genesis for 3.1.3

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var valueToSerialize = value as HaParams;
            if (valueToSerialize.AssociatedHaMode == HaMode.Exactly)
            {
                serializer.Serialize(writer, valueToSerialize.ExactlyCount);
            }
            else if (valueToSerialize.AssociatedHaMode == HaMode.Nodes && valueToSerialize.Nodes != null)
            {
                serializer.Serialize(writer, valueToSerialize.Nodes);
            }
            else
            {
                throw new JsonSerializationException("Could not serialize ha-params object");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            HaParams returnValue = null;
            var deserializationErrorMessage = "Could not read ha-params value";
            if (reader.TokenType == JsonToken.Integer)
            {
                returnValue = new HaParams {AssociatedHaMode = HaMode.Exactly, ExactlyCount = (long)reader.Value};
            }else if (reader.TokenType == JsonToken.StartArray)
            {
                var potentialReturnValue = new HaParams {AssociatedHaMode = HaMode.Nodes};
                var nodesList = new List<string>();
                do
                {
                    reader.Read();
                    if (!new[] {JsonToken.EndArray, JsonToken.String}.Contains(reader.TokenType))
                    {
                        deserializationErrorMessage = "Could not read ha-params array value";
                    }
                    else if(reader.TokenType == JsonToken.String)
                    {
                        nodesList.Add(reader.Value as string);
                    }
                } while (reader.TokenType == JsonToken.String);
                potentialReturnValue.Nodes = nodesList.ToArray();
                returnValue = potentialReturnValue;
            }
            if (returnValue != null)
            {
                return returnValue;
            }
            throw new JsonSerializationException(deserializationErrorMessage);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(HaParams);
        }
    }
}