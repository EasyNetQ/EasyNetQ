using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using Ninject;

namespace EasyNetQ.DI
{
	public class NinjectMessageDispatcher : IAutoSubscriberMessageDispatcher
	{
		private readonly IKernel _kernel;

		public NinjectMessageDispatcher(IKernel kernel)
		{
			this._kernel = kernel;
		}

		public void Dispatch<TMessage, TConsumer>(TMessage message)
			where TMessage : class
			where TConsumer : IConsume<TMessage>
		{
			_kernel.Get<TConsumer>().Consume(message);
		}

		public Task DispatchAsync<TMessage, TConsumer>(TMessage message)
			where TMessage : class
			where TConsumer : IConsumeAsync<TMessage>
		{
			var consumer = _kernel.Get<TConsumer>();
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
