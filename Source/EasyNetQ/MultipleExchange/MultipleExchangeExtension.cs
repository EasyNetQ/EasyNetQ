using EasyNetQ.DI;
using EasyNetQ.Producer;

namespace EasyNetQ.MultipleExchange
{
    public static class MultipleExchangeExtension
    {
        public static IServiceRegister EnableAdvancedMessagePolymorphism(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .Register<IPublishExchangeDeclareStrategy, MultipleExchangePublishExchangeDeclareStrategy>();
        }
    }
}
