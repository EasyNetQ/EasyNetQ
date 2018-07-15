using EasyNetQ.Events;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer
{
    public delegate AckResult AckStrategy(IModel model, ulong deliveryTag);

    public static class AckStrategies
    {
        public static AckStrategy Ack = (model, tag) => { model.BasicAck(tag, false); return AckResult.Ack; };
        public static AckStrategy NackWithoutRequeue = (model, tag) => { model.BasicNack(tag, false, false); return AckResult.Nack; };
        public static AckStrategy NackWithRequeue = (model, tag) => { model.BasicNack(tag, false, true); return AckResult.Nack; };
    }
}