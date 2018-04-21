using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using Ninject;

namespace EasyNetQ.DI.Ninject
{
	public class NinjectMessageDispatcher : IAutoSubscriberMessageDispatcher
	{
		private readonly IKernel kernel;

		public NinjectMessageDispatcher(IKernel kernel)
		{
			this.kernel = kernel;
		}

		public void Dispatch<TMessage, TConsumer>(TMessage message)
			where TMessage : class
			where TConsumer : IConsume<TMessage>
		{
			kernel.Get<TConsumer>().Consume(message);
		}

		public async Task DispatchAsync<TMessage, TConsumer>(TMessage message)
			where TMessage : class
			where TConsumer : IConsumeAsync<TMessage>
		{
			await kernel.Get<TConsumer>().Consume(message).ConfigureAwait(false);
		}
	}
}
