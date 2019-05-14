using EasyNetQ.DI;

namespace EasyNetQ
{
    public static class LegacyRpcConventionsExtensions
    {
        public static IServiceRegister EnableLegacyRpcConventions(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IConventions, LegacyRpcConventions>();
        }
    }
}
