using System;

namespace EasyNetQ
{
    /// <summary>
    ///     Responsible for serialization/deserialization of messages types
    /// </summary>
    public interface ITypeNameSerializer
    {
        /// <summary>
        ///     Serialize a message type
        /// </summary>
        /// <param name="type">The message type</param>
        /// <returns>The serialized message type</returns>
        string Serialize(Type type);

        /// <summary>
        ///     Deserialize a message type
        /// </summary>
        /// <param name="typeName">The serialized message type</param>
        /// <returns>The message type</returns>
        Type DeSerialize(string typeName);
    }
}
