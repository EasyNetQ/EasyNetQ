using System.Diagnostics;
using System.Reflection;
using System.Text;
using RabbitMQ.Client;

namespace EasyNetQ.OTEL
{
#warning ВРЕМЕННОЕ РЕШЕНИЕ для внедрения ParentContext-a в Activity при получении сообщения из шины.
#warning В RabbitMQ.Client добавили аналогичное в версии 7.2.0.-alpha. При выходе из альфа-версии стоит перейти на оригинальное решение
    public static class CustomRabbitMQActivitySource
    {
        // These constants are defined in the OpenTelemetry specification:
        // https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/#messaging-attributes
        internal const string MessageId = "messaging.message.id";
        internal const string MessageConversationId = "messaging.message.conversation_id";
        internal const string MessagingOperation = "messaging.operation";
        internal const string MessagingOperationNameBasicDeliver = "deliver";
        internal const string MessagingOperationName = "messaging.operation.name";
        internal const string MessagingOperationTypeProcess = "process";
        internal const string MessagingSystem = "messaging.system";
        internal const string MessagingOperationType = "messaging.operation.type";
        internal const string MessagingDestination = "messaging.destination.name";
        internal const string MessagingDestinationRoutingKey = "messaging.rabbitmq.destination.routing_key";
        internal const string MessagingBodySize = "messaging.message.body.size";
        internal const string MessagingEnvelopeSize = "messaging.message.envelope.size";
        internal const string ProtocolName = "network.protocol.name";
        internal const string ProtocolVersion = "network.protocol.version";
        internal const string RabbitMQDeliveryTag = "messaging.rabbitmq.delivery_tag";

        private static readonly string AssemblyVersion = typeof(CustomRabbitMQActivitySource).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "";

        private static readonly ActivitySource s_publisherSource =
            new ActivitySource(PublisherSourceName, AssemblyVersion);

        private static readonly ActivitySource s_subscriberSource =
            new ActivitySource(SubscriberSourceName, AssemblyVersion);

        public const string PublisherSourceName = "EasyNetQ.OTEL.Publisher";
        public const string SubscriberSourceName = "EasyNetQ.OTEL.Subscriber";

        public static Action<Activity, IDictionary<string, object>> ContextInjector { get; set; } = DefaultContextInjector;

        public static Func<IReadOnlyBasicProperties, ActivityContext> ContextExtractor { get; set; } =
            DefaultContextExtractor;

        public static bool UseRoutingKeyAsOperationName { get; set; } = true;
        internal static bool PublisherHasListeners => s_publisherSource.HasListeners();
        internal static bool SubscriberHasListeners => s_subscriberSource.HasListeners();

        internal static readonly IEnumerable<KeyValuePair<string, object>> CreationTags = new[]
        {
            new KeyValuePair<string, object>(MessagingSystem, "rabbitmq"),
            new KeyValuePair<string, object>(ProtocolName, "amqp"),
            new KeyValuePair<string, object>(ProtocolVersion, "0.9.1")
        };

        internal static Activity ReceiveEmpty(string queue)
        {
            if (!s_subscriberSource.HasListeners())
            {
                return null;
            }

            Activity activity = s_subscriberSource.StartRabbitMQActivity(
                UseRoutingKeyAsOperationName ? $"{queue} receive" : "receive",
                ActivityKind.Consumer);
            if (activity.IsAllDataRequested)
            {
                activity
                    .SetTag(MessagingOperation, "receive")
                    .SetTag(MessagingDestination, "amq.default");
            }

            return activity;
        }

        internal static Activity Receive(string routingKey, string exchange, ulong deliveryTag,
            in ReadOnlyBasicProperties readOnlyBasicProperties, int bodySize)
        {
            if (!s_subscriberSource.HasListeners())
            {
                return null;
            }

            // Extract the PropagationContext of the upstream parent from the message headers.
            Activity activity = s_subscriberSource.StartLinkedRabbitMQActivity(
                UseRoutingKeyAsOperationName ? $"{routingKey} receive" : "receive", ActivityKind.Consumer,
                parentContext: ContextExtractor(readOnlyBasicProperties));
            if (activity.IsAllDataRequested)
            {
                PopulateMessagingTags("receive", routingKey, exchange, deliveryTag, readOnlyBasicProperties,
                    bodySize, activity);
            }

            return activity;
        }

        internal static Activity Deliver(string routingKey, string exchange, ulong deliveryTag,
            IReadOnlyBasicProperties basicProperties, int bodySize)
        {
            try
            {
                if (!s_subscriberSource.HasListeners())
                {
                    return null;
                }

                var context = ContextExtractor(basicProperties);

                if (context.TraceId == default || string.IsNullOrWhiteSpace(context.TraceId.ToString()))
                {
                    context = new ActivityContext(
                        ActivityTraceId.CreateRandom(),
                        ActivitySpanId.CreateRandom(),
                        ActivityTraceFlags.None);
                }

                // Extract the PropagationContext of the upstream parent from the message headers.
                Activity activity = s_subscriberSource.StartLinkedRabbitMQActivity(
                    UseRoutingKeyAsOperationName ? $"{MessagingOperationNameBasicDeliver} {routingKey}" : MessagingOperationNameBasicDeliver,
                    ActivityKind.Consumer, linkedContext: context, parentContext: context);
                if (activity != null && activity.IsAllDataRequested)
                {
                    PopulateMessagingTags(MessagingOperationTypeProcess, MessagingOperationNameBasicDeliver, routingKey, exchange,
                        deliveryTag, basicProperties, bodySize, activity);
                }

                return activity;
            }
            catch
            {
                return null;
            }

        }

        private static void PopulateMessagingTags(string operationType, string operationName, string routingKey, string exchange,
            ulong deliveryTag, IReadOnlyBasicProperties readOnlyBasicProperties, int bodySize, Activity activity)
        {
            PopulateMessagingTags(operationType, operationName, routingKey, exchange, deliveryTag, bodySize, activity);

            if (!string.IsNullOrEmpty(readOnlyBasicProperties.CorrelationId))
            {
                activity.SetTag(MessageConversationId, readOnlyBasicProperties.CorrelationId);
            }

            if (!string.IsNullOrEmpty(readOnlyBasicProperties.MessageId))
            {
                activity.SetTag(MessageId, readOnlyBasicProperties.MessageId);
            }
        }

        private static void PopulateMessagingTags(string operationType, string operationName, string routingKey, string exchange,
            ulong deliveryTag, int bodySize, Activity activity)
        {
            activity
                .SetTag(MessagingOperationType, operationType)
                .SetTag(MessagingOperationName, operationName)
                .SetTag(MessagingDestination, string.IsNullOrEmpty(exchange) ? "amq.default" : exchange)
                .SetTag(MessagingDestinationRoutingKey, routingKey)
                .SetTag(MessagingBodySize, bodySize);

            if (deliveryTag > 0)
            {
                activity.SetTag(RabbitMQDeliveryTag, deliveryTag);
            }
        }

        private static Activity StartRabbitMQActivity(this ActivitySource source, string name, ActivityKind kind,
            ActivityContext parentContext = default)
        {
            Activity activity = source
                .CreateActivity(name, kind, parentContext, idFormat: ActivityIdFormat.W3C, tags: CreationTags)?.Start();
            return activity;
        }

        private static Activity StartLinkedRabbitMQActivity(this ActivitySource source, string name, ActivityKind kind,
            ActivityContext linkedContext = default, ActivityContext parentContext = default)
        {
            Activity activity = source.CreateActivity(name, kind, parentContext: parentContext,
                    links: new[] { new ActivityLink(linkedContext) }, idFormat: ActivityIdFormat.W3C,
                    tags: CreationTags)
                ?.Start();
            return activity;
        }

        private static void PopulateMessagingTags(string operation, string routingKey, string exchange,
            ulong deliveryTag, in IReadOnlyBasicProperties readOnlyBasicProperties, int bodySize, Activity activity)
        {
            PopulateMessagingTags(operation, routingKey, exchange, deliveryTag, bodySize, activity);

            if (!string.IsNullOrEmpty(readOnlyBasicProperties.CorrelationId))
            {
                activity.SetTag(MessageConversationId, readOnlyBasicProperties.CorrelationId);
            }

            if (!string.IsNullOrEmpty(readOnlyBasicProperties.MessageId))
            {
                activity.SetTag(MessageId, readOnlyBasicProperties.MessageId);
            }
        }

        private static void PopulateMessagingTags(string operation, string routingKey, string exchange,
            ulong deliveryTag, int bodySize, Activity activity)
        {
            activity
                .SetTag(MessagingOperation, operation)
                .SetTag(MessagingDestination, string.IsNullOrEmpty(exchange) ? "amq.default" : exchange)
                .SetTag(MessagingDestinationRoutingKey, routingKey)
                .SetTag(MessagingBodySize, bodySize);

            if (deliveryTag > 0)
            {
                activity.SetTag(RabbitMQDeliveryTag, deliveryTag);
            }
        }

        internal static void PopulateMessageEnvelopeSize(Activity activity, int size)
        {
            if (activity != null && activity.IsAllDataRequested && PublisherHasListeners)
            {
                activity.SetTag(MessagingEnvelopeSize, size);
            }
        }

        private static void DefaultContextInjector(Activity sendActivity, IDictionary<string, object> props)
        {
            props ??= new Dictionary<string, object>();
            DistributedContextPropagator.Current.Inject(sendActivity, props, DefaultContextSetter);
        }

        private static ActivityContext DefaultContextExtractor(IReadOnlyBasicProperties props)
        {
            if (props.Headers == null)
            {
                return default;
            }

            bool hasHeaders = false;
            foreach (string header in DistributedContextPropagator.Current.Fields)
            {
                if (props.Headers.ContainsKey(header))
                {
                    hasHeaders = true;
                    break;
                }
            }


            if (!hasHeaders)
            {
                return default;
            }

            DistributedContextPropagator.Current.ExtractTraceIdAndState(props.Headers, DefaultContextGetter, out string traceParent, out string traceState);
            return ActivityContext.TryParse(traceParent, traceState, out ActivityContext context) ? context : default;
        }

        private static void DefaultContextSetter(object carrier, string name, string value)
        {
            if (!(carrier is IDictionary<string, object> carrierDictionary))
            {
                return;
            }

            // Only propagate headers if they haven't already been set
            carrierDictionary[name] = value;
        }

        private static void DefaultContextGetter(object carrier, string name, out string value,
            out IEnumerable<string> values)
        {
            if (carrier is IDictionary<string, object> carrierDict &&
                carrierDict.TryGetValue(name, out object propsVal) && propsVal is byte[] bytes)
            {
                value = Encoding.UTF8.GetString(bytes);
                values = default;
            }
            else
            {
                value = default;
                values = default;
            }
        }
    }
}
