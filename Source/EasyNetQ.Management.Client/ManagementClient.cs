using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client
{
    public class ManagementClient : IManagementClient
    {
        private readonly string hostUrl;
        private readonly string username;
        private readonly string password;
        private readonly int portNumber;

        public ManagementClient(
            string hostUrl, 
            string username, 
            string password) : this(hostUrl, username, password, 15672)
        {
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
            if(connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            Delete(string.Format("connections/{0}", connection.name));
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
                SanitiseVhostName(vhost.name), exchangeName));
        }

        public Queue GetQueue(string queueName, Vhost vhost)
        {
            return Get<Queue>(string.Format("queues/{0}/{1}",
                SanitiseVhostName(vhost.name), queueName));
        }

        public Exchange CreateExchange(ExchangeInfo exchangeInfo, Vhost vhost)
        {
            if(exchangeInfo == null)
            {
                throw new ArgumentNullException("exchangeInfo");
            }
            if(vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            Put(string.Format("exchanges/{0}/{1}", SanitiseVhostName(vhost.name), exchangeInfo.GetName()), exchangeInfo);

            return GetExchange(exchangeInfo.GetName(), vhost);
        }

        public void DeleteExchange(Exchange exchange)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }

            Delete(string.Format("exchanges/{0}/{1}", SanitiseVhostName(exchange.vhost), exchange.name));
        }

        public IEnumerable<Binding> GetBindingsWithSource(Exchange exchange)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }

            return Get<IEnumerable<Binding>>(string.Format("exchanges/{0}/{1}/bindings/source", SanitiseVhostName(exchange.vhost), exchange.name));
        }

        public IEnumerable<Binding> GetBindingsWithDestination(Exchange exchange)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }

            return Get<IEnumerable<Binding>>(string.Format("exchanges/{0}/{1}/bindings/destination", SanitiseVhostName(exchange.vhost), exchange.name));
        }

        public PublishResult Publish(Exchange exchange, PublishInfo publishInfo)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if(publishInfo == null)
            {
                throw new ArgumentNullException("publishInfo");
            }

            return Post<PublishInfo, PublishResult>(
                string.Format("exchanges/{0}/{1}/publish", SanitiseVhostName(exchange.vhost), exchange.name), 
                publishInfo);
        }

        public IEnumerable<Queue> GetQueues()
        {
            return Get<IEnumerable<Queue>>("queues");
        }

        public Queue CreateQueue(QueueInfo queueInfo, Vhost vhost)
        {
            if(queueInfo == null)
            {
                throw new ArgumentNullException("queueInfo");
            }
            if(vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            Put(string.Format("queues/{0}/{1}", SanitiseVhostName(vhost.name), queueInfo.GetName()), queueInfo);

            return GetQueue(queueInfo.GetName(), vhost);
        }

        public void DeleteQueue(Queue queue)
        {
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            Delete(string.Format("queues/{0}/{1}", SanitiseVhostName(queue.vhost), queue.name));
        }

        public IEnumerable<Binding> GetBindingsForQueue(Queue queue)
        {
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return Get<IEnumerable<Binding>>(
                string.Format("queues/{0}/{1}/bindings", SanitiseVhostName(queue.vhost), queue.name));
        }

        public void Purge(Queue queue)
        {
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            Delete(string.Format("queues/{0}/{1}/contents", SanitiseVhostName(queue.vhost), queue.name));
        }

        public IEnumerable<Message> GetMessagesFromQueue(Queue queue, GetMessagesCriteria criteria)
        {
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return Post<GetMessagesCriteria, IEnumerable<Message>>(
                string.Format("queues/{0}/{1}/get", SanitiseVhostName(queue.vhost), queue.name),
                criteria);
        }

        public IEnumerable<Binding> GetBindings()
        {
            return Get<IEnumerable<Binding>>("bindings");
        }

        public void CreateBinding(Exchange exchange, Queue queue, BindingInfo bindingInfo)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }
            if(bindingInfo == null)
            {
                throw new ArgumentNullException("bindingInfo");
            }

            Post<BindingInfo, object>(
                string.Format("bindings/{0}/e/{1}/q/{2}", SanitiseVhostName(queue.vhost), exchange.name, queue.name), 
                bindingInfo);
        }

        public IEnumerable<Binding> GetBindings(Exchange exchange, Queue queue)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if(queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return Get<IEnumerable<Binding>>(
                string.Format("bindings/{0}/e/{1}/q/{2}", SanitiseVhostName(queue.vhost),
                    exchange.name, queue.name));
        }

        public void DeleteBinding(Binding binding)
        {
            if(binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            Delete(string.Format("bindings/{0}/e/{1}/q/{2}/{3}", 
                SanitiseVhostName(binding.vhost),
                binding.source, 
                binding.destination, 
                RecodeBindingPropertiesKey(binding.properties_key)));
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
            if(string.IsNullOrEmpty(virtualHostName))
            {
                throw new ArgumentException("virtualHostName is null or empty");
            }

            Put(string.Format("vhosts/{0}", virtualHostName));

            return GetVhost(virtualHostName);
        }

        public void DeleteVirtualHost(Vhost vhost)
        {
            if(vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            Delete(string.Format("vhosts/{0}", vhost.name));
        }

        public IEnumerable<User> GetUsers()
        {
            return Get<IEnumerable<User>>("users");
        }

        public User GetUser(string userName)
        {
            return Get<User>(string.Format("users/{0}", userName));
        }

        public User CreateUser(UserInfo userInfo)
        {
            if(userInfo == null)
            {
                throw new ArgumentNullException("userInfo");
            }

            Put(string.Format("users/{0}", userInfo.GetName()), userInfo);

            return GetUser(userInfo.GetName());
        }

        public void DeleteUser(User user)
        {
            if(user == null)
            {
                throw new ArgumentNullException("user");
            }

            Delete(string.Format("users/{0}", user.name));
        }


        public IEnumerable<Permission> GetPermissions()
        {
            return Get<IEnumerable<Permission>>("permissions");
        }

        public void CreatePermission(PermissionInfo permissionInfo)
        {
            if(permissionInfo == null)
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
            if(permission == null)
            {
                throw new ArgumentNullException("permission");
            }

            Delete(string.Format("permissions/{0}/{1}",
                permission.vhost,
                permission.user));
        }

        public bool IsAlive(Vhost vhost)
        {
            if(vhost == null)
            {
                throw new ArgumentNullException("vhost");
            }

            var result = Get<AlivenessTestResult>(string.Format("aliveness-test/{0}",
                SanitiseVhostName(vhost.name)));

            return result.status == "ok";
        }

        private T Get<T>(string path)
        {
            var request = CreateRequestForPath(path);

            var response = (HttpWebResponse) request.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new EasyNetQManagementException("Request failed with status code {0}", response.StatusCode);
            }

            var responseBody = GetBodyFromResponse(response);

            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        private TResult Post<TItem, TResult>(string path, TItem item)
        {
            var request = CreateRequestForPath(path);
            request.Method = "POST";

            InsertRequestBody(request, item);

            var response = (HttpWebResponse)request.GetResponse();

            var responseBody = GetBodyFromResponse(response);

            return JsonConvert.DeserializeObject<TResult>(responseBody);
        }

        private void Delete(string path)
        {
            var request = CreateRequestForPath(path);
            request.Method = "DELETE";

            try
            {
                var response = (HttpWebResponse) request.GetResponse();
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new EasyNetQManagementException("Unexpected status code: {0}", response.StatusCode);
                }
            }
            catch (WebException webException)
            {
                throw new EasyNetQManagementException("Unexpected status code: {0}",
                    ((HttpWebResponse)webException.Response).StatusCode);
            }
        }

        private void Put(string path)
        {
            var request = CreateRequestForPath(path);
            request.Method = "PUT";
            request.ContentType = "application/json";

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new EasyNetQManagementException("Unexpected status code: {0}", response.StatusCode);
                }
            }
            catch (WebException webException)
            {
                // GetBodyFromResponse((HttpWebResponse)webException.Response);
                throw new EasyNetQManagementException("Unexpected status code: {0}",
                    ((HttpWebResponse)webException.Response).StatusCode);
            }
        }

        private void Put<T>(string path, T item)
        {
            var request = CreateRequestForPath(path);
            request.Method = "PUT";

            InsertRequestBody(request, item);

            try
            {
                var response = (HttpWebResponse) request.GetResponse();
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new EasyNetQManagementException("Unexpected status code: {0}", response.StatusCode);
                }
            }
            catch (WebException webException)
            {
                // GetBodyFromResponse((HttpWebResponse)webException.Response);
                throw new EasyNetQManagementException("Unexpected status code: {0}",
                    ((HttpWebResponse)webException.Response).StatusCode);
            }
        }

        private static void InsertRequestBody<T>(HttpWebRequest request, T item)
        {
            request.ContentType = "application/json";
            var body = JsonConvert.SerializeObject(item);
            Console.WriteLine(body);
            using (var requestStream = request.GetRequestStream())
            using (var writer = new StreamWriter(requestStream))
            {
                writer.Write(body);
            }
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
            Console.WriteLine(responseBody);
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