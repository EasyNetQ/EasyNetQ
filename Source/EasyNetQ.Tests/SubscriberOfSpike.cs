using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace EasyNetQ.Tests
{
    public class SubscriberOfSpike
    {
        public void Should_be_able_to_auto_register_subscribers()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            AutoRegisterSubscribers(bus, Assembly.GetExecutingAssembly());

            // wait for the bus to come online
            while(!bus.IsConnected) Thread.Sleep(10);

            bus.Publish(new MyAutoSubscribedMessage{ Text = "Hello Message!"});
            bus.Publish(new MyOtherAutoSubscribeMessage { Text = "Other hello message!" });

            Thread.Sleep(100);
        }

        private void AutoRegisterSubscribers(IBus bus, Assembly assembly)
        {
            var genericSubscribeMethod = bus.GetType().GetMethod("Subscribe");

            var subscriberInfos = FindSubscribers(assembly);

            foreach (var subscriberInfo in subscriberInfos)
            {
                var subscribeMethod = genericSubscribeMethod.MakeGenericMethod(new[] {subscriberInfo.MessageType});
                
                var subscriberInstance = Activator.CreateInstance(subscriberInfo.SubscriberType);
                
                var subscriberId = subscriberInfo
                    .SubscriberType
                    .GetProperty("SubscriberId")
                    .GetValue(subscriberInstance, null);

                var handleMethod = subscriberInfo.SubscriberType.GetMethod("Handle");

                var delegateType = typeof (Action<>).MakeGenericType(subscriberInfo.MessageType);
                var handleDelegate = Delegate.CreateDelegate(delegateType, subscriberInstance, handleMethod);

                subscribeMethod.Invoke(bus, new[] { subscriberId, handleDelegate });
            }
        }

        public IEnumerable<SubscriberInfo> FindSubscribers(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var subscriberOfInterface = type
                    .GetInterfaces()
                    .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ISubscriberOf<>));

                if (subscriberOfInterface == null) continue;

                var messageType = subscriberOfInterface.GetGenericArguments()[0];
                yield return new SubscriberInfo(type, messageType);
            }
        }
    }

    public class SubscriberInfo
    {
        public SubscriberInfo(Type subscriberType, Type messageType)
        {
            SubscriberType = subscriberType;
            MessageType = messageType;
        }

        public Type SubscriberType { get; private set; }
        public Type MessageType { get; private set; }
    }

    public interface ISubscriberOf<T> where T : class
    {
        string SubscriberId { get; }
        void Handle(T message);
    }

    public class MyAutoSubscribedMessage
    {
        public string Text { get; set; }
    }

    public class MyOtherAutoSubscribeMessage
    {
        public string Text { get; set; }
    }

    public class MySubscriber : ISubscriberOf<MyAutoSubscribedMessage>
    {
        public string SubscriberId
        {
            get { return "subscriberId"; }
        }

        public void Handle(MyAutoSubscribedMessage message)
        {
            Console.Out.WriteLine("MySubscriber got MyAutoSubscribedMessage");
            Console.Out.WriteLine("message.Text = {0}", message.Text);
        }
    }

    public class MySecondSubscriber : ISubscriberOf<MyOtherAutoSubscribeMessage>
    {
        public string SubscriberId
        {
            get { return "subsriber2Id"; }
        }

        public void Handle(MyOtherAutoSubscribeMessage message)
        {
            Console.Out.WriteLine("MySecondSubscriber got MyOtherAutoSubscribeMessage");
            Console.Out.WriteLine("message.Text = {0}", message.Text);
        }
    }
}