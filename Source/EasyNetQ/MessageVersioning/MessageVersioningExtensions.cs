using EasyNetQ.DI;
using EasyNetQ.Producer;

namespace EasyNetQ.MessageVersioning
{
    public static class MessageVersioningExtensions
    {
         public static IServiceRegister EnableMessageVersioning( this IServiceRegister serviceRegister )
         {
             return serviceRegister
                 .Register<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>()
                 .Register<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
         }
    }
}
