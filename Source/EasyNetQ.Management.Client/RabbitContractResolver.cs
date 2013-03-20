using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;

namespace EasyNetQ.Management.Client
{
    public class RabbitContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return Regex.Replace(propertyName, "([a-z])([A-Z])", "$1_$2").ToLower();
        }
    }
}
