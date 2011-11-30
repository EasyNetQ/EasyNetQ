using System.Threading.Tasks;

namespace EasyNetQ.Tests.Sagas
{
    public class AsyncSubscribePublishSaga : ISaga
    {
        public void Initialize(IBus bus)
        {
            bus.SubscribeAsync<TestRequestMessage>("id", requestMessage =>
            {
                var responseMessage = new TestResponseMessage
                {
                    Text = requestMessage.Text
                };
                return Task.Factory.StartNew(() => bus.Publish(responseMessage));
            });
        }
    }
}