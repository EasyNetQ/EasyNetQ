using EasyNetQ.DI;

namespace EasyNetQ
{
    public static class LegacyTypeNamingExtensions
    {
        public static IServiceRegister EnableLegacyTypeNaming(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<ITypeNameSerializer, LegacyTypeNameSerializer>();
        }   
    }
}