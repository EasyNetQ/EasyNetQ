using System.Collections;
using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;
using Queue = EasyNetQ.Management.Client.Model.Queue;

namespace EasyNetQ.Management.Client
{
    public interface IManagementClient
    {
        /// <summary>
        /// Various random bits of information that describe the whole system.
        /// </summary>
        /// <returns></returns>
        Overview GetOverview();

        /// <summary>
        /// A list of nodes in the RabbitMQ cluster.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Node> GetNodes();

        /// <summary>
        /// The server definitions - exchanges, queues, bindings, users, virtual hosts, permissions. 
        /// Everything apart from messages.
        /// </summary>
        /// <returns></returns>
        Definitions GetDefinitions();

        /// <summary>
        /// A list of all open connections.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Connection> GetConnections();

        /// <summary>
        /// A list of all open channels.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Channel> GetChannels();

        /// <summary>
        /// A list of all exchanges.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Exchange> GetExchanges();

        /// <summary>
        /// A list of all queues.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Queue> GetQueues();

        /// <summary>
        /// A list of all bindings.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Binding> GetBindings();

        /// <summary>
        /// A list of all vhosts.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Vhost> GetVHosts();

        /// <summary>
        /// A list of all users.
        /// </summary>
        /// <returns></returns>
        IEnumerable<User> GetUsers();

        /// <summary>
        /// A list of all permissions for all users.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Permission> GetPermissions();

        /// <summary>
        /// Closes the given connection
        /// </summary>
        /// <param name="connection"></param>
        void CloseConnection(Connection connection);

        /// <summary>
        /// Creates the given exchange
        /// </summary>
        /// <param name="exchangeInfo"></param>
        void CreateExchange(ExchangeInfo exchangeInfo, Vhost vhost);

        /// <summary>
        /// Delete the given exchange
        /// </summary>
        /// <param name="exchange"></param>
        void DeleteExchange(Exchange exchange);

        /// <summary>
        /// A list of all bindings in which a given exchange is the source.
        /// </summary>
        /// <param name="exchange"></param>
        /// <returns></returns>
        IEnumerable<Binding> GetBindingsWithSource(Exchange exchange);

        /// <summary>
        /// A list of all bindings in which a given exchange is the destination.
        /// </summary>
        /// <param name="exchange"></param>
        /// <returns></returns>
        IEnumerable<Binding> GetBindingsWithDestination(Exchange exchange);

        /// <summary>
        /// Publish a message to a given exchange.
        /// 
        /// Please note that the publish / get paths in the HTTP API are intended for injecting 
        /// test messages, diagnostics etc - they do not implement reliable delivery and so should 
        /// be treated as a sysadmin's tool rather than a general API for messaging.
        /// </summary>
        /// <param name="exchange">The exchange</param>
        /// <param name="publishInfo">The publication parameters</param>
        /// <returns>A PublishResult, routed == true if the message was sent to at least one queue</returns>
        PublishResult Publish(Exchange exchange, PublishInfo publishInfo);

        /// <summary>
        /// Create the given queue
        /// </summary>
        /// <param name="queueInfo"></param>
        /// <param name="vhost"></param>
        void CreateQueue(QueueInfo queueInfo, Vhost vhost);

        /// <summary>
        /// Delete the given queue
        /// </summary>
        /// <param name="queue"></param>
        void DeleteQueue(Queue queue);

        /// <summary>
        /// A list of all bindings on a given queue.
        /// </summary>
        /// <param name="queue"></param>
        IEnumerable<Binding> GetBindingsForQueue(Queue queue);

        /// <summary>
        /// Purge a queue of all messages
        /// </summary>
        /// <param name="queue"></param>
        void Purge(Queue queue);

        /// <summary>
        /// Get messages from a queue.
        /// 
        /// Please note that the publish / get paths in the HTTP API are intended for 
        /// injecting test messages, diagnostics etc - they do not implement reliable 
        /// delivery and so should be treated as a sysadmin's tool rather than a 
        /// general API for messaging.
        /// </summary>
        /// <param name="queue">The queue to retrieve from</param>
        /// <param name="criteria">The criteria for the retrieve</param>
        /// <returns>Messages</returns>
        IEnumerable<Message> GetMessagesFromQueue(Queue queue, GetMessagesCriteria criteria);

        /// <summary>
        /// Create a binding between an exchange and a queue
        /// </summary>
        /// <param name="exchange">the exchange</param>
        /// <param name="queue">the queue</param>
        /// <param name="bindingInfo">properties of the binding</param>
        /// <returns>The binding that was created</returns>
        void CreateBinding(Exchange exchange, Queue queue, BindingInfo bindingInfo);

        /// <summary>
        /// A list of all bindings between an exchange and a queue. 
        /// Remember, an exchange and a queue can be bound together many times!
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        IEnumerable<Binding> GetBindings(Exchange exchange, Queue queue);

        /// <summary>
        /// Delete the given binding
        /// </summary>
        /// <param name="binding"></param>
        void DeleteBinding(Binding binding);
    }
}