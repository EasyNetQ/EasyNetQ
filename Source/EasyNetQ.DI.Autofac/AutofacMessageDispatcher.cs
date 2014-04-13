﻿using System.Threading.Tasks;
using Autofac;
using EasyNetQ.AutoSubscribe;

namespace EasyNetQ.DI
{
    public class AutofacMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly ILifetimeScope component;

        public AutofacMessageDispatcher(ILifetimeScope component)
        {
            this.component = component;
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            using (var scope = component.BeginLifetimeScope("message"))
                scope.Resolve<TConsumer>().Consume(message);
        }

        public Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            var scope = component.BeginLifetimeScope("async-message");
            var consumer = scope.Resolve<TConsumer>();
            return consumer
                .Consume(message)
                .ContinueWith(task => scope.Dispose());
        }
    }
}