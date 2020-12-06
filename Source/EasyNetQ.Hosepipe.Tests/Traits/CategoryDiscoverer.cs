using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace EasyNetQ.Hosepipe.Tests.Traits
{
    /// <summary>
    /// Adapted from xUnit sample on Trait extensibility
    /// https://github.com/xunit/samples.xunit/blob/master/TraitExtensibility/CategoryDiscoverer.cs
    /// </summary>
    public class CategoryDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            var namedArgs = traitAttribute.GetNamedArgument<Category>(nameof(CategoryAttribute.Category));

            if (namedArgs == Category.None)
            {
                yield break;
            }
            else
            {
                yield return new KeyValuePair<string, string>("Category", namedArgs.ToString());
            }
        }
    }
}
