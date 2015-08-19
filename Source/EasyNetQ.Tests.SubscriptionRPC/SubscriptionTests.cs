using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Tests.SubscriptionRPC.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class TestRequest {
    public int SenderId { get; set; }
}

public class TestResponse {
    public int SenderId { get; set; }
}

namespace EasyNetQ.Tests.SubscriptionRPC 
{
    [TestClass]
    public class SubscriptionTests 
    {
        [TestMethod]
        public void TestMethod1() {
            var endpoint = "unit-test-queue";

            var bus = RabbitHutch.CreateBus("host=localhost;persistentMessages=false;prefetchcount=30;timeout=20", service => {
                service.Register<ITypeNameSerializer, NameSerialiser>();
                service.Register<IEasyNetQLogger>(_ => new EasyNetQ.Loggers.ConsoleLogger { Debug = false, Info = false, Error = true });
            });

            IDisposable token = null;

            try {
                //Setup first subscriber
                token = bus.RespondAsync(endpoint, Handler(1));
                var task = bus.RequestAsync<TestResponse>(endpoint, new TestRequest { SenderId = 2 }, TimeSpan.FromSeconds(5));

                Task.WaitAll(task);
                var result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.SenderId);
            }
            finally {
                bus.Dispose();
                token.Dispose();
            }
        }

        [TestMethod]
        public void TestMethod2() {
            var endpoint = "unit-test-queue";
            //var topic = "topic";
            var topic1 = "topic.different";
            var topic2 = "topic.another";
            //var topic3 = "topic.unused";

            var bus = RabbitHutch.CreateBus("host=localhost;persistentMessages=false;prefetchcount=30;timeout=20", service => {
                service.Register<ITypeNameSerializer, NameSerialiser>();
                service.Register<IEasyNetQLogger>(_ => new EasyNetQ.Loggers.ConsoleLogger { Debug = true, Info = true, Error = true });
            });

            var token = new List<IDisposable>();

            try {
                //Setup first subscriber
                //token.Add(bus.RespondAsync(endpoint, Handler(1), topic2));
                token.Add(bus.RespondAsync(endpoint, Handler(1), Guid.NewGuid().ToString(), config => config.WithAutoDelete().WithTopic(topic2)));
                token.Add(bus.RespondAsync(endpoint, Handler(2), Guid.NewGuid().ToString(), config => config.WithAutoDelete().WithTopic(topic1)));
                var task = bus.RequestAsync<TestResponse>(endpoint, new TestRequest { SenderId = 3 }, TimeSpan.FromSeconds(3), topic1);

                Task.WaitAll(task);
                var result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(2, result.SenderId);
            }
            finally {
                bus.Dispose();
                token.Each(v => v.Dispose());
            }
        }

        private Func<TestRequest, Task<TestResponse>> Handler(int senderId) {
            return msg => {
                Console.WriteLine("Received Request from {0}", senderId);
                return Task.FromResult(new TestResponse() { SenderId = senderId });
            };
        }
    }
}
