using System;

namespace EasyNetQ
{
    public interface ISerializer
    {
        byte[] MessageToBytes(Type messageType, object message);
        object BytesToMessage(Type messageType, byte[] bytes);
    }
}