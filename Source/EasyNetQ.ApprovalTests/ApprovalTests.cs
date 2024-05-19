using PublicApiGenerator;

namespace EasyNetQ.Approval.Tests;

public class ApprovalTests
{
    [Theory]
    [InlineData(typeof(RabbitBus))]
    [InlineData(typeof(DI.LightInject.LightInjectAdapter))]
    [InlineData(typeof(DI.Microsoft.ServiceCollectionAdapter))]
    [InlineData(typeof(Serialization.NewtonsoftJson.NewtonsoftJsonSerializer))]
    [InlineData(typeof(Serialization.SystemTextJson.SystemTextJsonSerializer))]
    public void Public_api_should_not_be_changed_unintentionally(Type type)
    {
        var publicApi = type?.Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            IncludeAssemblyAttributes = false,
            AllowNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
            ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" },
        });

        publicApi.ShouldMatchApproved(options => options.WithFilenameGenerator((_, _, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
    }
}
