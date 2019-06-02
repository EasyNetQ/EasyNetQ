using EasyNetQ.DI;

namespace EasyNetQ
{
    public static class ConventionsExtensions
    {
        /// <summary>
        /// Shortcut for EnableLegacyTypeNaming() + EnableLegacyRpcConventions()
        /// </summary>
        /// <param name="serviceRegister">Object for registering services</param>
        public static IServiceRegister EnableLegacyConventions(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .EnableLegacyTypeNaming()
                .EnableLegacyRpcConventions();
        }
    }
}
