using System;
using System.Threading;

namespace EasyNetQ.Tests
{
    public class SubscribeSpike
    {
        public void WhatTopologyHappensOnSubscribe()
        {
            var bus = RabbitHutch.CreateBus("localhost");
            bus.Subscribe<WhyAreThereTwo>("duplicateTest", _ => Console.WriteLine("Got Message"));    

            bus.Publish(new WhyAreThereTwo());

            Thread.Sleep(1000);
        }
    }

    [Serializable]
    public class WhyAreThereTwo
    {
        
    }
}