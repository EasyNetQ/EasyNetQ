using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using SimpleInjector;

namespace EasyNetQ.DI
{
    public class SimpleInjectorMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        private readonly Container _container;

        public SimpleInjectorMessageDispatcher(Container kernel)
        {
            this._container = kernel;
        }

        public void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            var instance = (IConsume<TMessage>)_container.GetInstance(typeof(TConsumer));
            instance.Consume(message);
        }

        public Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            var consumer = (IConsumeAsync<TMessage>)_container.GetInstance(typeof(TConsumer));
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
