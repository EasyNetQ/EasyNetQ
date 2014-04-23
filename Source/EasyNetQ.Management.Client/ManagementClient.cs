﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using EasyNetQ.Management.Client.Model;
using EasyNetQ.Management.Client.Serialization;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client
{
    using System.Diagnostics.Contracts;
    using Newtonsoft.Json.Converters;

    public class ManagementClient : IManagementClient
    {
        private readonly string hostUrl;
        private readonly string username;
        private readonly string password;
        private readonly int portNumber;
        public static readonly JsonSerializerSettings Settings;

        public ManagementClient(
            string hostUrl,
            string username,
            string password)
            : this(hostUrl, username, password, 15672)
        {
        }

        static ManagementClient()
        {
            Settings = new JsonSerializerSettings
            {
                ContractResolver = new RabbitContractResolver(),
            };

            Settings.Converters.Add(new PropertyConverter());
            Settings.Converters.Add(new MessageStatsOrEmptyArrayConverter());
            Settings.Converters.Add(new QueueTotalsOrEmptyArrayConverter());
            Settings.Converters.Add(new StringEnumConverter { CamelCaseText = true});
            Settings.Converters.Add(new HaParamsConverter());
        }

        public string HostUrl
        {
            get { return hostUrl; }
        }

        public string Username
        {
            get { return username; }
        }

        public int PortNumber
        {
            get { return portNumber; }
        }

        public ManagementClient(string hostUrl, string username, string password, int portNumber)
        {
            if (string.IsNullOrEmpty(hostUrl))
            {
                throw new ArgumentException("hostUrl is null or empty");
            }
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("username is null or empty");
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("password is null or empty");
            }

            this.hostUrl = hostUrl;
            this.username = username;
            this.password = password;
            this.portNumber = portNumber;

            

            LeaveDotsAndSlashesEscaped();
        }

        public Overview GetOverview()
        {
            return Get<Overview>("overview");
        }

        public IEnumerable<Node> GetNodes()
        {
            return Get<IEnumerable<Node>>("nodes");
        }

        public Definitions GetDefinitions()
        {
            return Get<Definitions>("definitions");
        }

        public IEnumerable<Connection> GetConnections()
        {
            return Get<IEnumerable<Connection>>("connections");
        }

        public void CloseConnection(Connection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            Delete(string.Format("connections/{0}", connection.Name));
        }

        public IEnumerable<Channel> GetChannels()
        {
            return Get<IEnumerable<Channel>>("channels");
        }

        public IEnumerable<Exchange> GetExchanges()
        {
            return Get<IEnumerable<Exchange>>("exchanges");
        }

        public Exchange GetExchange(string exchangeName, Vhost vhost)
        {
            return Get<Exchange>(string.Format("exchanges/{0}/{1}",
                SanitiseVhostName(vhost.Name), exchangeName));
        }

        public Queue GetQueue(string queueName, Vhost vhost)
        {
            return Get<Queue>(string.Format("queues/{0}/{1}",
                SanitiseVhostName(vhost.Name), SanitiseQueueName(queueName)));
        }

        public Exchange CreateExchange(ExchangeInfo exchangeInfo, Vhost vhost)
        {
            if (exchangeInfo == null)
            {
                throw new ArgumentNullException("exchangeInfo");
            }
            if (vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            Put(string.Format("exchanges/{0}/{1}", SanitiseVhostName(vhost.Name), exchangeInfo.GetName()), exchangeInfo);

            return GetExchange(exchangeInfo.GetName(), vhost);
        }

        public void DeleteExchange(Exchange exchange)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }

            Delete(string.Format("exchanges/{0}/{1}", SanitiseVhostName(exchange.Vhost), exchange.Name));
        }

        public IEnumerable<Binding> GetBindingsWithSource(Exchange exchange)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }

            return Get<IEnumerable<Binding>>(string.Format("exchanges/{0}/{1}/bindings/source", SanitiseVhostName(exchange.Vhost), exchange.Name));
        }

        public IEnumerable<Binding> GetBindingsWithDestination(Exchange exchange)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }

            return Get<IEnumerable<Binding>>(string.Format("exchanges/{0}/{1}/bindings/destination", SanitiseVhostName(exchange.Vhost), exchange.Name));
        }

        public PublishResult Publish(Exchange exchange, PublishInfo publishInfo)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if (publishInfo == null)
            {
                throw new ArgumentNullException("publishInfo");
            }

            return Post<PublishInfo, PublishResult>(
                string.Format("exchanges/{0}/{1}/publish", SanitiseVhostName(exchange.Vhost), exchange.Name),
                publishInfo);
        }

        public IEnumerable<Queue> GetQueues()
        {
            return Get<IEnumerable<Queue>>("queues");
        }

        public Queue CreateQueue(QueueInfo queueInfo, Vhost vhost)
        {
            if (queueInfo == null)
            {
                throw new ArgumentNullException("queueInfo");
            }
            if (vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            Put(string.Format("queues/{0}/{1}", SanitiseVhostName(vhost.Name), SanitiseQueueName(queueInfo.GetName())), queueInfo);

            return GetQueue(queueInfo.GetName(), vhost);
        }

        public void DeleteQueue(Queue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            Delete(string.Format("queues/{0}/{1}", SanitiseVhostName(queue.Vhost), SanitiseQueueName(queue.Name)));
        }

        public IEnumerable<Binding> GetBindingsForQueue(Queue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return Get<IEnumerable<Binding>>(
                string.Format("queues/{0}/{1}/bindings", SanitiseVhostName(queue.Vhost), SanitiseQueueName(queue.Name)));
        }

        public void Purge(Queue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            Delete(string.Format("queues/{0}/{1}/contents", SanitiseVhostName(queue.Vhost), SanitiseQueueName(queue.Name)));
        }

        public IEnumerable<Message> GetMessagesFromQueue(Queue queue, GetMessagesCriteria criteria)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return Post<GetMessagesCriteria, IEnumerable<Message>>(
                string.Format("queues/{0}/{1}/get", SanitiseVhostName(queue.Vhost), SanitiseQueueName(queue.Name)),
                criteria);
        }

        public IEnumerable<Binding> GetBindings()
        {
            return Get<IEnumerable<Binding>>("bindings");
        }

        public void CreateBinding(Exchange exchange, Queue queue, BindingInfo bindingInfo)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }
            if (bindingInfo == null)
            {
                throw new ArgumentNullException("bindingInfo");
            }

            Post<BindingInfo, object>(
                string.Format("bindings/{0}/e/{1}/q/{2}", SanitiseVhostName(queue.Vhost), exchange.Name, SanitiseQueueName(queue.Name)),
                bindingInfo);
        }

        public void CreateBinding(Exchange sourceExchange, Exchange destinationExchange, BindingInfo bindingInfo)
        {
            if (sourceExchange == null)
            {
                throw new ArgumentNullException("sourceExchange");
            }
            if (destinationExchange == null)
            {
                throw new ArgumentNullException("destinationExchange");
            }
            if (bindingInfo == null)
            {
                throw new ArgumentNullException("bindingInfo");
            }

            Post<BindingInfo, object>(
                string.Format("bindings/{0}/e/{1}/e/{2}", SanitiseVhostName(sourceExchange.Vhost), sourceExchange.Name, destinationExchange.Name),
                bindingInfo);
        }

        public IEnumerable<Binding> GetBindings(Exchange exchange, Queue queue)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return Get<IEnumerable<Binding>>(
                string.Format("bindings/{0}/e/{1}/q/{2}", SanitiseVhostName(queue.Vhost),
                    exchange.Name, SanitiseQueueName(queue.Name)));
        }

        public void DeleteBinding(Binding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            Delete(string.Format("bindings/{0}/e/{1}/q/{2}/{3}",
                SanitiseVhostName(binding.Vhost),
                binding.Source,
                binding.Destination,
                RecodeBindingPropertiesKey(binding.PropertiesKey)));
        }

        public IEnumerable<Vhost> GetVHosts()
        {
            return Get<IEnumerable<Vhost>>("vhosts");
        }

        public Vhost GetVhost(string vhostName)
        {
            return Get<Vhost>(string.Format("vhosts/{0}", SanitiseVhostName(vhostName)));
        }

        public Vhost CreateVirtualHost(string virtualHostName)
        {
            if (string.IsNullOrEmpty(virtualHostName))
            {
                throw new ArgumentException("virtualHostName is null or empty");
            }

            Put(string.Format("vhosts/{0}", virtualHostName));

            return GetVhost(virtualHostName);
        }

        public void DeleteVirtualHost(Vhost vhost)
        {
            if (vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            Delete(string.Format("vhosts/{0}", vhost.Name));
        }

        public IEnumerable<User> GetUsers()
        {
            return Get<IEnumerable<User>>("users");
        }

        public User GetUser(string userName)
        {
            return Get<User>(string.Format("users/{0}", userName));
        }

        public IEnumerable<Policy> GetPolicies()
        {
            return Get<IEnumerable<Policy>>("policies");
        }

        public void CreatePolicy(Policy policy)
        {
            if (string.IsNullOrEmpty(policy.Name))
            {
                throw new ArgumentException("Policy name is empty");
            }
            if (string.IsNullOrEmpty(policy.Vhost))
            {
                throw new ArgumentException("vhost name is empty");
            }
            if (policy.Definition == null)
            {
                throw new ArgumentException("Definition should not be null");
            }

            Put(GetPolicyUrl(policy.Name, policy.Vhost), policy);
        }

        private string GetPolicyUrl(string policyName, string vhost)
        {
            return string.Format("policies/{0}/{1}", SanitiseVhostName(vhost), policyName);
        }

        public void DeletePolicy(string policyName, Vhost vhost)
        {
            Delete(GetPolicyUrl(policyName, vhost.Name));
        }

        public IEnumerable<Parameter> GetParameters()
        {
            return Get<IEnumerable<Parameter>>("parameters");
        }

        public void CreateParameter(Parameter parameter)
        {
            var componentName = parameter.Component;
            var vhostName = parameter.Vhost;
            var parameterName = parameter.Name;
            Put(GetParameterUrl(componentName, vhostName, parameterName), parameter);
        }

        private string GetParameterUrl(string componentName, string vhost, string parameterName)
        {
            return string.Format("parameters/{0}/{1}/{2}", componentName, SanitiseVhostName(vhost), parameterName);
        }

        public void DeleteParameter(string componentName, string vhost, string name)
        {
            Delete(GetParameterUrl(componentName, vhost, name));
        }

        public User CreateUser(UserInfo userInfo)
        {
            if (userInfo == null)
            {
                throw new ArgumentNullException("userInfo");
            }

            Put(string.Format("users/{0}", userInfo.GetName()), userInfo);

            return GetUser(userInfo.GetName());
        }

        public void DeleteUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            Delete(string.Format("users/{0}", user.Name));
        }

        public IEnumerable<Permission> GetPermissions()
        {
            return Get<IEnumerable<Permission>>("permissions");
        }

        public void CreatePermission(PermissionInfo permissionInfo)
        {
            if (permissionInfo == null)
            {
                throw new ArgumentNullException("permissionInfo");
            }

            Put(string.Format("permissions/{0}/{1}",
                    permissionInfo.GetVirtualHostName(),
                    permissionInfo.GetUserName()),
                permissionInfo);
        }

        public void DeletePermission(Permission permission)
        {
            if (permission == null)
            {
                throw new ArgumentNullException("permission");
            }

            Delete(string.Format("permissions/{0}/{1}",
                permission.Vhost,
                permission.User));
        }

        public User ChangeUserPassword(string userName, string newPassword)
        {
            var user = GetUser(userName);
            var tags = user.Tags.Split(',');
            var userInfo = new UserInfo(userName, newPassword);
            foreach (var tag in tags)
            {
                userInfo.AddTag(tag.Trim());
            }
            return CreateUser(userInfo);
        }

        public bool IsAlive(Vhost vhost)
        {
            if (vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            var result = Get<AlivenessTestResult>(string.Format("aliveness-test/{0}",
                SanitiseVhostName(vhost.Name)));

            return result.Status == "ok";
        }

        private T Get<T>(string path)
        {
            var request = CreateRequestForPath(path);

            var response = request.GetHttpResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);
            }

            return DeserializeResponse<T>(response);
        }

        private TResult Post<TItem, TResult>(string path, TItem item)
        {
            var request = CreateRequestForPath(path);
            request.Method = "POST";

            InsertRequestBody(request, item);

            var response = request.GetHttpResponse();
            if (!(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created))
            {
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);
            }

            return DeserializeResponse<TResult>(response);
        }

        private void Delete(string path)
        {
            var request = CreateRequestForPath(path);
            request.Method = "DELETE";

            var response = request.GetHttpResponse();
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);
            }
        }

        private void Put(string path)
        {
            var request = CreateRequestForPath(path);
            request.Method = "PUT";
            request.ContentType = "application/json";

            var response = request.GetHttpResponse();
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);
            }
        }

        private void Put<T>(string path, T item)
        {
            var request = CreateRequestForPath(path);
            request.Method = "PUT";

            InsertRequestBody(request, item);

            var response = request.GetHttpResponse();
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);
            }
        }

        private void InsertRequestBody<T>(HttpWebRequest request, T item)
        {
            request.ContentType = "application/json";

            var body = JsonConvert.SerializeObject(item, Settings);
            using (var requestStream = request.GetRequestStream())
            using (var writer = new StreamWriter(requestStream))
            {
                writer.Write(body);
            }
        }
        
        private T DeserializeResponse<T>(HttpWebResponse response)
        {
            var responseBody = GetBodyFromResponse(response);
            return JsonConvert.DeserializeObject<T>(responseBody, Settings);
        }

        private static string GetBodyFromResponse(HttpWebResponse response)
        {
            string responseBody;
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    throw new EasyNetQManagementException("Response stream was null");
                }
                using (var reader = new StreamReader(responseStream))
                {
                    responseBody = reader.ReadToEnd();
                }
            }
            return responseBody;
        }

        private HttpWebRequest CreateRequestForPath(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(BuildEndpointAddress(path));
            request.Credentials = new NetworkCredential(username, password);
            return request;
        }

        private string BuildEndpointAddress(string path)
        {
            return string.Format("{0}:{1}/api/{2}", hostUrl, portNumber, path);
        }

        private string SanitiseVhostName(string vhostName)
        {
            return vhostName.Replace("/", "%2f");
        }

        private string SanitiseQueueName(string queueName)
        {
            return queueName.Replace("+", "%2B");
        }

        private string RecodeBindingPropertiesKey(string propertiesKey)
        {
            return propertiesKey.Replace("%5F", "%255F");
        }

        /// <summary>
        /// See http://mikehadlow.blogspot.co.uk/2011/08/how-to-stop-systemuri-un-escaping.html
        /// </summary>
        private void LeaveDotsAndSlashesEscaped()
        {
            var getSyntaxMethod =
                typeof(UriParser).GetMethod("GetSyntax", BindingFlags.Static | BindingFlags.NonPublic);
            if (getSyntaxMethod == null)
            {
                throw new MissingMethodException("UriParser", "GetSyntax");
            }

            var uriParser = getSyntaxMethod.Invoke(null, new object[] { "http" });

            var setUpdatableFlagsMethod =
                uriParser.GetType().GetMethod("SetUpdatableFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            if (setUpdatableFlagsMethod == null)
            {
                throw new MissingMethodException("UriParser", "SetUpdatableFlags");
            }

            setUpdatableFlagsMethod.Invoke(uriParser, new object[] { 0 });
        }
    }
}