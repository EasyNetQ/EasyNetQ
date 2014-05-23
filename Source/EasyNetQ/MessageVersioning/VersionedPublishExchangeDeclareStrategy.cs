using System;
using System.Collections.Concurrent;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MessageVersioning
{
    public class VersionedPublishExchangeDeclareStrategy : IPublishExchangeDeclareStrategy
    {
        private readonly ConcurrentDictionary<string, IExchange> exchangeNames = new ConcurrentDictionary<string, IExchange>();

        public IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType)
        {
            return exchangeNames.AddOrUpdate(
                exchangeName,
                name => advancedBus.ExchangeDeclare(name, exchangeType),
                (_, exchange) => exchange);
        }

        public IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType)
        {
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var messageVersions = new MessageVersionStack( messageType );
            var publishExchange = DeclareVersionedExchanges( advancedBus, conventions, messageVersions, exchangeType );
            return publishExchange;
        }

        private IExchange DeclareVersionedExchanges( IAdvancedBus advancedBus, IConventions conventions, MessageVersionStack messageVersions, string exchangeType )
        {
            // This works because the message version stack is LIFO from most superseded message type to the actual message type 
            IExchange destinationExchange = null;
            while( !messageVersions.IsEmpty() )
            {
                var messageType = messageVersions.Pop();
                var exchangeName = conventions.ExchangeNamingConvention( messageType );
                var sourceExchange = DeclareExchange( advancedBus, exchangeName, exchangeType );

                if( destinationExchange != null )
                    advancedBus.Bind( sourceExchange, destinationExchange, "#" );

                destinationExchange = sourceExchange;
            }
            return destinationExchange;
        }
    }
}