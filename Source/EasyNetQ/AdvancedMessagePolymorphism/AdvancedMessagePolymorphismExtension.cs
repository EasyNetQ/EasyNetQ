using EasyNetQ.Producer;

namespace EasyNetQ.AdvancedMessagePolymorphism
{
    public static class AdvancedMessagePolymorphismExtension
    {
        public static IServiceRegister EnableAdvancedMessagePolymorphism(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .Register<IPublishExchangeDeclareStrategy, AdvancedMessagePolymorphismPublishExchangeDeclareStrategy>();
        }
    }
}
