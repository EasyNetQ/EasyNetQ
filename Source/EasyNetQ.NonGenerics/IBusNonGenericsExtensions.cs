using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EasyNetQ.NonGenerics
{
    public static class IBusNonGenericsExtensions
    {
        public static void Subscribe(this IBus bus, Type messageType, string subscriptionId, Action<object> onMessage, Action<SubscriptionConfiguration<object>> configure = null)
        {
            var advancedBus = bus.Advanced;

            var messageHandler = MessageHandlerFor(bus, messageType, onMessage);

            var exchangeName = bus.ExchangeNameFor(messageType);

            var queue = advancedBus.QueueDeclare(QueueNameFor(bus, messageType, subscriptionId));
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            var configuration = new SubscriptionConfiguration<object>();
            if (configure != null)
                configure(configuration);

            if (configuration.Topics.Count == 0)
            {
                advancedBus.Bind(exchange, queue, "#");
            }
            else
            {
                foreach (var topic in configuration.Topics)
                {
                    advancedBus.Bind(exchange, queue, topic);
                }
            }

            advancedBus.Consume(queue, messageHandler);
        }

        private static Func<byte[], MessageProperties, MessageReceivedInfo, Task> MessageHandlerFor(IBus bus, Type messageType, Action<object> onMessage)
        {
            return new Func<byte[], MessageProperties, MessageReceivedInfo, Task>((body, properties, info) =>
            {
                try
                {
                    var serializer = bus.Advanced.Serializer;

                    //var messageBody = serializer.BytesToMessage(messageType.FullName + ":" + messageType.Assembly.GetName().Name, body);

                    var serializerType = serializer.GetType();
                    var bytesToMessageGenericMethodInfo = serializerType.GetMethods().Single(m => m.Name == "BytesToMessage" && m.IsGenericMethod);
                    var bytesToMessageConcreteMethodInfo = bytesToMessageGenericMethodInfo.MakeGenericMethod(messageType);
                    var messageBody = bytesToMessageConcreteMethodInfo.Invoke(serializer, new object[] { body });
                    var easyNetQMessageType = typeof(EasyNetQ.Message<>).MakeGenericType(messageType);
                    var easyNetQMessage = Activator.CreateInstance(easyNetQMessageType, new object[] { messageBody });
                    var setPropertiesOnEasyNetQMessage = easyNetQMessageType.GetMethod("SetProperties");
                    setPropertiesOnEasyNetQMessage.Invoke(easyNetQMessage, new object[] { properties });

                    return Task.Run(() =>
                    {
                        onMessage(messageBody);
                    });
                }
                catch
                {
                    throw;
                }
            });
        }

        private static Conventions Conventions = new Conventions(new TypeNameSerializer());

        private static string ExchangeNameFor(this IBus bus, Type messageType)
        {
            // Uma vez que o EasyNetQ não expõe as convensões usadas, assume-se aqui que as padrões foram usadas. Caso não seja, isso deve ser alterado.
            var exchangeName = Conventions.ExchangeNamingConvention(messageType);
            return exchangeName;
        }

        public static string QueueNameFor(this IBus bus, Type messageType, string subscriptionId)
        {
            // Uma vez que o EasyNetQ não expõe as convensões usadas, assume-se aqui que as padrões foram usadas. Caso não seja, isso deve ser alterado.
            var queueName = Conventions.QueueNamingConvention(messageType, subscriptionId);
            return queueName;
        }

        private static Dictionary<Type, MethodInfo> PublishMethods = new Dictionary<Type, MethodInfo>();

        public static Task PublishAsync(this IBus bus, string topic, object message)
        {
            return PublishAsyncUsingReflection(bus, topic, message);
        }

        private static Task PublishAsyncUsingAdvancedBus(IBus bus, string topic, object message)
        {
            var advancedBus = bus.Advanced;
            var messageType = message.GetType();
            var exchangeName = Conventions.ExchangeNamingConvention(messageType);
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            var easyNetQMessageType = typeof(EasyNetQ.Message<>).MakeGenericType(messageType);
            var easyNetQMessage = Activator.CreateInstance(easyNetQMessageType, new object[] { message });

            var typeName = advancedBus.TypeNameSerializer.Serialize(messageType);
            var messageBody = advancedBus.Serializer.MessageToBytes(message);

            var properties = new MessageProperties
            {
                DeliveryMode = 2,
                Type = typeName,
                CorrelationId = CorrelationIdGenerator.GetCorrelationId()
            };
            var setPropertiesOnEasyNetQMessage = easyNetQMessageType.GetMethod("SetProperties");
            setPropertiesOnEasyNetQMessage.Invoke(easyNetQMessage, new object[] { properties });

            var publishingTask = advancedBus.PublishAsync(exchange, topic, false, false, properties, messageBody);
            return publishingTask;
        }

        private static Task PublishAsyncUsingReflection(IBus bus, string topic, object message)
        {
            var parameterTypes = String.IsNullOrEmpty(topic)
                ? new Type[] { typeof(object) }
                : new Type[] { typeof(object), typeof(string) };

            var parameters = String.IsNullOrEmpty(topic)
                ? new object[] { message }
                : new object[] { message, topic };

            var publishMethod = MakePublishMethod(bus.GetType(), parameterTypes, parameters);

            return (Task)publishMethod.Invoke(bus, parameters);
        }

        private static MethodInfo MakePublishMethod(Type busType, object[] parameterTypes, object[] parameters)
        {
            var message = parameters[0];
            var messageType = message.GetType();
            MethodInfo publishConcreteMethod;

            if (!PublishMethods.TryGetValue(messageType, out publishConcreteMethod))
            {
                var publishAsyncGenericMethod = busType.GetMethods()
                    .Single(m =>
                        m.Name == "PublishAsync" &&
                        m.IsGenericMethod &&
                        m.GetParameters().Length == parameterTypes.Length
                    );
                publishConcreteMethod = publishAsyncGenericMethod.MakeGenericMethod(messageType);
                PublishMethods[messageType] = publishConcreteMethod;
            }

            return publishConcreteMethod;
        }
    }
}
