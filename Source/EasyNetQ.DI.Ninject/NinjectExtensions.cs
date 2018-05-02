using System;
using Ninject;

namespace EasyNetQ.DI.Ninject
{
    public static class NinjectExtensions
    {
        public static IKernel RegisterEasyNetQ(this IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.RegisterEasyNetQ(c => {});
        }
        
        public static IKernel RegisterEasyNetQ(this IKernel kernel, Action<IServiceRegister> registerServices)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }
            
            if (registerServices == null)
            {
                throw new ArgumentNullException(nameof(registerServices));
            }
            
            var serviceRegistry = new NinjectAdapter(kernel);
            serviceRegistry.RegisterDefaultServices();
            registerServices(serviceRegistry);
            return kernel;
        }
    }
}