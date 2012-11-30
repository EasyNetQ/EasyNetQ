using System.ComponentModel;
using System.Configuration;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using System.Linq;

namespace EasyNetQ.Monitor
{
    public class AppSettingsConvention : ISubDependencyResolver
    {
        public bool CanResolve(
            CreationContext context,
            ISubDependencyResolver contextHandlerResolver,
            ComponentModel model,
            DependencyModel dependency)
        {
            return ConfigurationManager.AppSettings.AllKeys.Contains(dependency.DependencyKey)
                && TypeDescriptor
                    .GetConverter(dependency.TargetType)
                    .CanConvertFrom(typeof(string));
        }

        public object Resolve(
            CreationContext context,
            ISubDependencyResolver contextHandlerResolver,
            ComponentModel model,
            DependencyModel dependency)
        {
            return TypeDescriptor
                .GetConverter(dependency.TargetType)
                .ConvertFrom(
                    ConfigurationManager.AppSettings[dependency.DependencyKey]);
        }
    }
}