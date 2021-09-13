using System;
using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace EasyNetQ.Approval.Tests
{
    public class ApprovalTests
    {
        [Theory]
        [InlineData(typeof(RabbitBus))]
        [InlineData(typeof(DI.Autofac.AutofacAdapter))]
        [InlineData(typeof(DI.LightInject.LightInjectAdapter))]
        [InlineData(typeof(DI.Microsoft.ServiceCollectionAdapter))]
        [InlineData(typeof(DI.Ninject.NinjectAdapter))]
        [InlineData(typeof(DI.SimpleInjector.SimpleInjectorAdapter))]
        [InlineData(typeof(DI.StructureMap.StructureMapAdapter))]
        [InlineData(typeof(DI.Windsor.WindsorAdapter))]
        public void Public_api_should_not_be_changed_unintentionally(Type type)
        {
            string publicApi = type?.Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                WhitelistedNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" },
            });

            publicApi.ShouldMatchApproved(options => options.WithFilenameGenerator((_, __, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
        }
    }
}
