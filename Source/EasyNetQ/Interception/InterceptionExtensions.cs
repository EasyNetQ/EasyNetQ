using System;
using EasyNetQ.DI;

namespace EasyNetQ.Interception
{
    public static class InterceptionExtensions
    {
        public static IServiceRegister EnableInterception(this IServiceRegister serviceRegister, Action<IInterceptorRegistrator> configure)
        {
            var registrator = new InterceptorRegistrator(serviceRegister);
            configure(registrator);
            return registrator.Register();
        }

        public static IInterceptorRegistrator EnableGZipCompression(this IInterceptorRegistrator interceptorRegistrator)
        {
            interceptorRegistrator.Add(new GZipInterceptor());
            return interceptorRegistrator;
        }

        public static IInterceptorRegistrator EnableTripleDESEncryption(this IInterceptorRegistrator interceptorRegistrator, byte[] key, byte[] iv)
        {
            interceptorRegistrator.Add(new TripleDESInterceptor(key, iv));
            return interceptorRegistrator;
        }
    }
}