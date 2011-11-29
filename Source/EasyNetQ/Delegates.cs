using System;

namespace EasyNetQ
{
    public delegate string SerializeType(Type type);
    public delegate string SubsriberNameFromDelegate(Delegate @delegate);
}