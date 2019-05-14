using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class LegacyRpcConventions : Conventions
    {
        public LegacyRpcConventions(ITypeNameSerializer typeNameSerializer)
            : base(typeNameSerializer)
        {
            RpcResponseExchangeNamingConvention = type => Exchange.GetDefault().Name;
        }
    }
}
