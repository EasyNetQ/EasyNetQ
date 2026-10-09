using EasyNetQ.Topology;

namespace EasyNetQ;

public class LegacyRpcConventions : Conventions
{
    public LegacyRpcConventions(ITypeNameSerializer typeNameSerializer, IMessageTypeRegistry messageTypeRegistry)
        : base(typeNameSerializer, messageTypeRegistry)
    {
        RpcResponseExchangeNamingConvention = _ => Exchange.Default.Name;
    }
}
