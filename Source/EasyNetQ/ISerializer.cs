using System;
using System.Buffers;

namespace EasyNetQ
{
    /// <summary>
    ///     Serializes message to bytes and back
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        ///     Serializes message to bytes
        /// </summary>
        IMemoryOwner<byte> MessageToBytes(Type messageType, object message);

        /// <summary>
        ///     Deserializes message from bytes
        /// </summary>
        object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes);
    }
}
