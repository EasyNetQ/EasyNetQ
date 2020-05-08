using System;
using System.Threading;
using EasyNetQ.Producer;

namespace EasyNetQ.Tests.Integration
{
    public class SimpleSagaSpike
    {
        /// <summary>
        /// Run both EasyNetQ.Tests.SimpleSaga and EasyNetQ.Tests.SimpleService first
        /// You should see the message hit the SimpleSaga, bounce to the SimpleService
        /// and then bounce back to the SimpleSaga again before ending here.
        /// </summary>
        public void RunSaga()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var bus = RabbitHutch.CreateBus("host=localhost");

            bus.PubSub.Subscribe<EndMessage>("runSaga_spike", endMessage =>
            {
                Console.WriteLine("Got EndMessage: {0}", endMessage.Text);
                autoResetEvent.Set();
            });

            var startMessage = new StartMessage
            {
                Text = "Hello Saga! "
            };

            bus.PubSub.Publish(startMessage);

            // give the message time to run through the process
            autoResetEvent.WaitOne(1000);
        }

        public void Can_call_publish_inside_a_subscribe_handler()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            // setup the Saga
            Console.WriteLine("Setting up the Saga");
            bus.PubSub.Subscribe<StartMessage>("simpleSaga", startMessage =>
            {
                Console.WriteLine("Saga got StartMessage: {0}", startMessage.Text);
                var firstProcessedMessage = startMessage.Text + " - initial process ";
                var request = new TestRequestMessage { Text = firstProcessedMessage };

                bus.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(request).ContinueWith(response =>
                {
                    Console.WriteLine("Saga got Response: {0}", response.Result.Text);
                    var secondProcessedMessage = response.Result.Text + " - final process ";
                    var endMessage = new EndMessage { Text = secondProcessedMessage };
                    bus.PubSub.Publish(endMessage);
                });
            });

            // setup the RPC endpoint
            Console.WriteLine("Setting up the RPC endpoint");
            bus.Rpc.Respond<TestRequestMessage, TestResponseMessage>(request =>
            {
                Console.WriteLine("RPC got Request: {0}", request.Text);
                return new TestResponseMessage { Text = request.Text + " Responded! " };
            });

            // setup the final subscription
            Console.WriteLine("Setting up the final subscription");
            var autoResetEvent = new AutoResetEvent(false);
            bus.PubSub.Subscribe<EndMessage>("inline_saga_spike", endMessage =>
            {
                Console.WriteLine("Test got EndMessage: {0}", endMessage.Text);
                autoResetEvent.Set();
            });

            // now kick it off
            Console.WriteLine("Test is publishing StartMessage");
            bus.PubSub.Publish(new StartMessage { Text = "Hello Saga!! " });

            // give the message time to run through the process
            autoResetEvent.WaitOne(1000);
        }
    }
}
