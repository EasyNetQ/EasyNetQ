using EasyNetQ.DI;

namespace EasyNetQ
{
    public static class ConventionsExtensions
    {
        /// <summary>
        /// Shortcut for EnableLegacyTypeNaming() + EnableLegacyRpcConventions()
        /// </summary>
        public static IServiceRegister EnableLegacyConventions(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .EnableLegacyTypeNaming()
                .EnableLegacyRpcConventions();
        }
    }
}
