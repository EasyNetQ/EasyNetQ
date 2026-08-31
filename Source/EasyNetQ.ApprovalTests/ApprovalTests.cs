using PublicApiGenerator;

namespace EasyNetQ.Approval.Tests;

public class ApprovalTests
{
    [Theory]
    [InlineData(typeof(Pipeline.PropertyBag))] // EasyNetQ.Core
    [InlineData(typeof(RabbitBus))] // EasyNetQ.RabbitMQ
    [InlineData(typeof(AutoSubscribe.AutoSubscriber))] // EasyNetQ (bundle)
    [InlineData(typeof(Serialization.NewtonsoftJson.NewtonsoftJsonSerializer))]
    public void Public_api_should_not_be_changed_unintentionally(Type type)
    {
        var publicApi = type?.Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            IncludeAssemblyAttributes = false,
            AllowNamespacePrefixes = ["Microsoft.Extensions.DependencyInjection"],
            ExcludeAttributes = ["System.Diagnostics.DebuggerDisplayAttribute"],
        });
        Assert.NotNull(publicApi);

        publicApi.ShouldMatchApproved(options => options.WithFilenameGenerator((_, _, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
    }
}
