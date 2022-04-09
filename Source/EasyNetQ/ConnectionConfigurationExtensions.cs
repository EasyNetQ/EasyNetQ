using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace EasyNetQ;

internal static class ConnectionConfigurationExtensions
{
    public static void SetDefaultProperties(this ConnectionConfiguration configuration)
    {
        Preconditions.CheckNotNull(configuration, nameof(configuration));

        if (configuration.Hosts.Count == 0)
            throw new EasyNetQException(
                "Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\""
            );

        foreach (var hostConfiguration in configuration.Hosts)
            if (hostConfiguration.Port == 0)
                hostConfiguration.Port = configuration.Port;

        var applicationNameAndPath = Environment.GetCommandLineArgs()[0];

        var applicationName = "unknown";
        var applicationPath = "unknown";
        if (!string.IsNullOrWhiteSpace(applicationNameAndPath))
            try
            {
                // Will only throw an exception if the applicationName contains invalid characters, is empty, or too long
                // Silently catch the exception, as we will just leave the application name and path to "unknown"
                applicationName = Path.GetFileName(applicationNameAndPath);
                applicationPath = Path.GetDirectoryName(applicationNameAndPath);
            }
            catch (ArgumentException)
            {
            }
            catch (PathTooLongException)
            {
            }

        AddValueIfNotExists(configuration.ClientProperties, "client_api", "EasyNetQ");
        AddValueIfNotExists(configuration.ClientProperties, "product", configuration.Product ?? applicationName);
        AddValueIfNotExists(configuration.ClientProperties, "platform", configuration.Platform ?? GetPlatform());
        AddValueIfNotExists(configuration.ClientProperties, "os", Environment.OSVersion.ToString());
        AddValueIfNotExists(configuration.ClientProperties, "version", GetApplicationVersion());
        AddValueIfNotExists(configuration.ClientProperties, "connection_name", configuration.Name ?? applicationName);
        AddValueIfNotExists(configuration.ClientProperties, "easynetq_version", typeof(ConnectionConfigurationExtensions).Assembly.GetName().Version.ToString());
        AddValueIfNotExists(configuration.ClientProperties, "application", applicationName);
        AddValueIfNotExists(configuration.ClientProperties, "application_location", applicationPath);
        AddValueIfNotExists(configuration.ClientProperties, "machine_name", Environment.MachineName);
        AddValueIfNotExists(configuration.ClientProperties, "user", configuration.UserName);
        AddValueIfNotExists(configuration.ClientProperties, "connected", DateTime.UtcNow.ToString("u")); // UniversalSortableDateTimePattern: yyyy'-'MM'-'dd HH':'mm':'ss'Z'
        AddValueIfNotExists(configuration.ClientProperties, "requested_heartbeat", configuration.RequestedHeartbeat.ToString());
        AddValueIfNotExists(configuration.ClientProperties, "timeout", configuration.Timeout.ToString());
        AddValueIfNotExists(configuration.ClientProperties, "publisher_confirms", configuration.PublisherConfirms.ToString());
        AddValueIfNotExists(configuration.ClientProperties, "persistent_messages", configuration.PersistentMessages.ToString());
    }

    private static void AddValueIfNotExists(IDictionary<string, object> clientProperties, string name, string value)
    {
        // allows to set nulls, null values will be displayed in RabbitMQ Management Plugin UI as 'undefined'
        if (!clientProperties.ContainsKey(name))
            clientProperties.Add(name, value);
    }

    private static string GetApplicationVersion()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string GetPlatform()
    {
        string platform = RuntimeInformation.FrameworkDescription;
        string frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (frameworkName != null)
            platform = platform + " [" + frameworkName + "]";

        // example: .NET Core 4.6.27317.07 [.NETCoreApp,Version=v2.0]
        return platform;
    }
}
