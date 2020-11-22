using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;
using Xunit;

namespace EasyNetQ.IntegrationTests
{
    public class RabbitMQFixture : IAsyncLifetime, IDisposable
    {
        private static readonly TimeSpan InitializationTimeout = TimeSpan.FromMinutes(2);

        private readonly DockerProxy dockerProxy;
        private OSPlatform dockerEngineOsPlatform;
        private string dockerNetworkName;

        public RabbitMQFixture()
        {
            dockerProxy = new DockerProxy(new Uri(Configuration.DockerHttpApiUri));
            Host = "localhost";
        }

        public string Host { get; private set; }

        public IManagementClient ManagementClient { get; private set; }

        public async Task InitializeAsync()
        {
            using var cts = new CancellationTokenSource(InitializationTimeout);
            dockerEngineOsPlatform = await dockerProxy.GetDockerEngineOsAsync(cts.Token);
            dockerNetworkName = dockerEngineOsPlatform == OSPlatform.Windows ? null : "bridgeWhaleNet";
            await DisposeAsync(cts.Token);
            await CreateNetworkAsync(cts.Token);
            var rabbitMQDockerImage = await PullImageAsync(cts.Token);
            var containerId = await RunNewContainerAsync(rabbitMQDockerImage, cts.Token);
            if (dockerEngineOsPlatform == OSPlatform.Windows)
                Host = await dockerProxy.GetContainerIpAsync(containerId, cts.Token);
            ManagementClient = new ManagementClient(Host, Configuration.RabbitMqUser, Configuration.RabbitMqPassword);
            await WaitForRabbitMqReadyAsync(cts.Token);
        }

        public async Task DisposeAsync()
        {
            await DisposeAsync(default);
        }

        public void Dispose()
        {
            dockerProxy.Dispose();
        }

        private async Task DisposeAsync(CancellationToken cancellationToken)
        {
            await dockerProxy.StopContainerAsync(Configuration.RabbitMqHostName, cancellationToken);
            await dockerProxy.RemoveContainerAsync(Configuration.RabbitMqHostName, cancellationToken);
            if (dockerEngineOsPlatform == OSPlatform.Linux || dockerEngineOsPlatform == OSPlatform.OSX)
                await dockerProxy.DeleteNetworkAsync(dockerNetworkName, cancellationToken);
        }

        private async Task CreateNetworkAsync(CancellationToken cancellationToken)
        {
            if (dockerEngineOsPlatform == OSPlatform.Linux || dockerEngineOsPlatform == OSPlatform.OSX)
                await dockerProxy.CreateNetworkAsync(dockerNetworkName, cancellationToken);
        }

        private async Task<string> PullImageAsync(CancellationToken cancellationToken)
        {
            var rabbitMQDockerImageName = Configuration.RabbitMQDockerImageName(dockerEngineOsPlatform);
            var rabbitMQDockerImageTag = Configuration.RabbitMQDockerImageTag(dockerEngineOsPlatform);
            await dockerProxy.PullImageAsync(rabbitMQDockerImageName, rabbitMQDockerImageTag, cancellationToken);
            return $"{rabbitMQDockerImageName}:{rabbitMQDockerImageTag}";
        }

        private async Task<string> RunNewContainerAsync(string rabbitMQDockerImage, CancellationToken cancellationToken)
        {
            var portMappings = new Dictionary<string, ISet<string>>
            {
                {"4369", new HashSet<string> {"4369"}},
                {"5671", new HashSet<string> {"5671"}},
                {"5672", new HashSet<string> {"5672"}},
                {"15671", new HashSet<string> {"15671"}},
                {"15672", new HashSet<string> {"15672"}},
                {"25672", new HashSet<string> {"25672"}}
            };
            var envVars = new List<string> {$"RABBITMQ_DEFAULT_VHOST={Configuration.RabbitMqVirtualHostName}"};
            var containerId = await dockerProxy
                .CreateContainerAsync(
                    rabbitMQDockerImage, Configuration.RabbitMqHostName, portMappings, dockerNetworkName, envVars,
                    cancellationToken
                );
            await dockerProxy.StartContainerAsync(containerId, cancellationToken);
            return containerId;
        }

        private async Task WaitForRabbitMqReadyAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsRabbitMqReadyAsync(cancellationToken))
                    return;
                await Task.Delay(500, cancellationToken);
            }
        }

        private async Task<bool> IsRabbitMqReadyAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await ManagementClient.IsAliveAsync(Configuration.RabbitMqVirtualHost, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
