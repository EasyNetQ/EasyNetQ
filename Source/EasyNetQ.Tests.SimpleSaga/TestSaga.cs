using System;
using System.ComponentModel.Composition;

namespace EasyNetQ.Tests.SimpleSaga
{
    [Export(typeof(ISaga))]
    public class TestSaga : ISaga
    {
        public void Initialize(IBus bus)
        {
            bus.Subscribe<StartMessage>("simpleSaga", startMessage =>
            {
                Console.WriteLine("StartMessage: {0}", startMessage.Text);
                var firstProcessedMessage = startMessage.Text + " - initial process ";
                var request = new TestRequestMessage { Text = firstProcessedMessage };
                using (var publishChannel = bus.OpenPublishChannel())
                {
                    publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    {
                        Console.WriteLine("TestResponseMessage: {0}", response.Text);
                        var secondProcessedMessage = response.Text + " - final process ";
                        var endMessage = new EndMessage {Text = secondProcessedMessage};
                        using (var publishChannel2 = bus.OpenPublishChannel())
                        {
                            publishChannel2.Publish(endMessage);
                        }
                    });
                }
            });
        }
    }
}