using System;
using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using LightInject;

namespace EasyNetQ.DI
{
    public class LightInjectMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly IServiceContainer _container;

        public LightInjectMessageDispatcher(IServiceContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this._container = container;
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            _container.GetInstance<TConsumer>().Consume(message);
        }

        public Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            var consumer = _container.GetInstance<TConsumer>();
            var tsc = new TaskCompletionSource<object>();
            consumer
                .Consume(message)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted && task.Exception != null)
                    {
                        tsc.SetException(task.Exception);
                    }
                    else
                    {
                        tsc.SetResult(null);
                    }
                });

            return tsc.Task;
        }
    }
}
