﻿using EasyNetQ.Loggers;

namespace EasyNetQ.InMemoryClient
{
    public class InMemoryRabbitHutch
    {
        public static IBus CreateBus()
        {
            var connectionFactory = new InMemoryConnectionFactory();

            var serializer = new JsonSerializer();
            var logger = new ConsoleLogger();
            var conventions = new Conventions();
            var consumerErrorStrategy = new DefaultConsumerErrorStrategy(connectionFactory, serializer, logger, conventions);
           

            var advancedBus = new RabbitAdvancedBus(
                new ConnectionConfiguration(), 
                connectionFactory,
                TypeNameSerializer.Serialize,
                serializer,
                new QueueingConsumerFactory(logger, consumerErrorStrategy),
                logger,
                CorrelationIdGenerator.GetCorrelationId,
                conventions);

            return new RabbitBus(
                TypeNameSerializer.Serialize,
                logger,
                conventions,
                advancedBus);            
        }
    }
}