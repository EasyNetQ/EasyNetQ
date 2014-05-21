using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;
using Queue = EasyNetQ.Management.Client.Model.Queue;

namespace EasyNetQ.Management.Client
{
    public interface IManagementClient
    {
        /// <summary>
        /// The host URL that this instance is using.
        /// </summary>
        string HostUrl { get; }

        /// <summary>
        /// The Username that this instance is connecting as.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// The port number this instance connects using.
        /// </summary>
        int PortNumber { get; }

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
		/// Gets the channel. This returns more detail, including consumers than the GetChannels method.
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="channelName">Channel name.</param>
		Channel GetChannel (string channelName);

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
        /// <param name="vhost"></param>
        Exchange CreateExchange(ExchangeInfo exchangeInfo, Vhost vhost);

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
        Queue CreateQueue(QueueInfo queueInfo, Vhost vhost);

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
        /// Create a binding between an exchange and an exchange
        /// </summary>
        /// <param name="sourceExchange">the source exchange</param>
        /// <param name="destinationExchange">the destination exchange</param>
        /// <param name="bindingInfo">properties of the binding</param>
        void CreateBinding(Exchange sourceExchange, Exchange destinationExchange, BindingInfo bindingInfo);

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

        /// <summary>
        /// Create a new virtual host
        /// </summary>
        /// <param name="virtualHostName">The name of the new virtual host</param>
        Vhost CreateVirtualHost(string virtualHostName);

        /// <summary>
        /// Delete a virtual host
        /// </summary>
        /// <param name="vhost">The virtual host to delete</param>
        void DeleteVirtualHost(Vhost vhost);

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="userInfo">The user to create</param>
        User CreateUser(UserInfo userInfo);

        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="user">The user to delete</param>
        void DeleteUser(User user);

        /// <summary>
        /// Create a permission
        /// </summary>
        /// <param name="permissionInfo">The permission to create</param>
        void CreatePermission(PermissionInfo permissionInfo);

        /// <summary>
        /// Delete a permission
        /// </summary>
        /// <param name="permission">The permission to delete</param>
        void DeletePermission(Permission permission);

        /// <summary>
        /// Update the password of an user.
        /// </summary>
        /// <param name="userName">The name of a user</param>
        /// <param name="newPassword">The new password to set</param>
        User ChangeUserPassword(string userName, string newPassword);

        /// <summary>
        /// Declares a test queue, then publishes and consumes a message. Intended for use 
        /// by monitoring tools. If everything is working correctly, will return true.
        /// Note: the test queue will not be deleted (to to prevent queue churn if this 
        /// is repeatedly pinged).
        /// </summary>
        bool IsAlive(Vhost vhost);

        /// <summary>
        /// Get an individual exchange by name
        /// </summary>
        /// <param name="exchangeName">The name of the exchange</param>
        /// <param name="vhost">The virtual host that contains the exchange</param>
        /// <returns>The exchange</returns>
        Exchange GetExchange(string exchangeName, Vhost vhost);

        /// <summary>
        /// Get an individual queue by name
        /// </summary>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="vhost">The virtual host that contains the queue</param>
        /// <returns>The Queue</returns>
        Queue GetQueue(string queueName, Vhost vhost);

        /// <summary>
        /// Get an individual vhost by name
        /// </summary>
        /// <param name="vhostName">The VHost</param>
        Vhost GetVhost(string vhostName);

        /// <summary>
        /// Get a user by name
        /// </summary>
        /// <param name="userName">The name of the user</param>
        /// <returns>The User</returns>
        User GetUser(string userName);

        /// <summary>
        /// Get collection of Policies on the cluster
        /// </summary>
        /// <returns>Policies</returns>
        IEnumerable<Policy> GetPolicies();

        /// <summary>
        /// Creates a policy on the cluster
        /// </summary>
        /// <param name="policy">Policy to create</param>
        void CreatePolicy(Policy policy);

        /// <summary>
        /// Delete a policy from the cluster
        /// </summary>
        /// <param name="policyName">Policy name</param>
        /// <param name="vhost">vhost on which the policy resides</param>
        void DeletePolicy(string policyName, Vhost vhost);

        /// <summary>
        /// Get all parameters on the cluster
        /// </summary>
        IEnumerable<Parameter> GetParameters();

        /// <summary>
        /// Creates a parameter on the cluster
        /// </summary>
        /// <param name="policy">Parameter to create</param>
        void CreateParameter(Parameter parameter);

        /// <summary>
        /// Delete a parameter from the cluster
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="vhost"></param>
        /// <param name="name"></param>
        void DeleteParameter(string componentName, string vhost, string name);
    }
}