using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
	public class MockConsumerFactory : IConsumerFactory
	{
		public void Dispose()
		{
		}

	    public DefaultBasicConsumer CreateConsumer(IModel model, bool modelIsSingleUse, MessageCallback callback)
	    {
	        return null;
	    }

	    public void ClearConsumers()
		{
		}
	}
}