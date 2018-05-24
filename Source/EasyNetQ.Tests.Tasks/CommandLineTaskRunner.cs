using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Net.Autofac.CommandLine;
using Serilog;
using Serilog.Events;

namespace EasyNetQ.Tests.Tasks
{
    public class CommandLineTaskRunner : IDisposable
    {
        private CancellationTokenSource cancellationTokenSource;
        private Autofac.IContainer container;

        private Task Run(CancellationToken cancellationToken)
        {
            return container
                .Resolve<DisplayCommandLineTasks>()
                .Run(cancellationToken);
        }

        public int Run()
        {
            var builder = CreateContainerBuilder();

            SetupLogging(builder);

            container = builder.Build();

            var cancellationToken = CreateCancellationToken();

            Task.WaitAll(Run(cancellationToken));

            return 0;
        }

        private static ContainerBuilder CreateContainerBuilder()
        {
            var assembliesToScan = new[]
            {
                typeof (Program).Assembly,
                typeof (DisplayCommandLineTasks).Assembly
            };

            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(assembliesToScan)
                .AsImplementedInterfaces()
                .AsSelf();

            return builder;
        }

        private static void SetupLogging(ContainerBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole(LogEventLevel.Debug)
                .CreateLogger();

            builder.RegisterInstance(Log.Logger).AsImplementedInterfaces().AsSelf();
        }

        private CancellationToken CreateCancellationToken()
        {
            Console.CancelKeyPress += (sender, args) =>
            {
                cancellationTokenSource.Cancel();
                args.Cancel = true;
            };

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            cancellationToken.Register(() => container.Resolve<ILogger>().Warning("Canceling"));
            return cancellationToken;
        }

        public void Dispose()
        {
            try
            {
                container.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}