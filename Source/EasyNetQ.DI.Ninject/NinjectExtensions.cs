﻿using System;
using Ninject;

namespace EasyNetQ.DI.Ninject
{
    public static class NinjectExtensions
    {
        public static IKernel RegisterEasyNetQ(this IKernel kernel, ConnectionConfiguration connectionConfiguration, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }
            
            var serviceRegistry = new NinjectAdapter(kernel);
            serviceRegistry.RegisterBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
            return kernel;
        }
    }
}