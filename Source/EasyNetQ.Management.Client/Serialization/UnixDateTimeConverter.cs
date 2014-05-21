using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace EasyNetQ.Management.Client.Serialization
{
    /// <summary>
    /// http://stackapps.com/questions/1175/how-to-convert-unix-timestamp-to-net-datetime/1176#1176
    /// 
    ///   Useful when serializing/deserializing json for use with the Stack Overflow API, which produces and consumes Unix Timestamp dates
    /// </summary>
    /// <remarks>
    ///   swiped from lfoust and fixed for latest json.net with some tweaks for handling out-of-range dates
    /// </remarks>
    public class UnixDateTimeConverter : DateTimeConverterBase
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            long val;
            if (value is DateTime)
            {
                val = ((DateTime)value).ToUnixTime();
            }
            else
            {
                throw new Exception("Expected date object value.");
            }
            writer.WriteValue(val);
        }

        /// <summary>
        ///   Reads the JSON representation of the object.
        /// </summary>
        /// <param name = "reader">The <see cref = "JsonReader" /> to read from.</param>
        /// <param name = "objectType">Type of the object.</param>
        /// <param name = "existingValue">The existing value of object being read.</param>
        /// <param name = "serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                                        JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Integer)
                throw new Exception("Wrong Token Type");

            long ticks = (long)reader.Value;
            return ticks.FromUnixTime();
        }
    }
}
