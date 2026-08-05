using Xunit.v3;

namespace EasyNetQ.Hosepipe.Tests.Traits;

/// <summary>
/// Apply this attribute to your test method to specify a category.
/// </summary>
/// <remarks>
/// From xUnit sample about Trait extensibility:
/// https://github.com/xunit/samples.xunit/blob/master/TraitExtensibility/CategoryAttribute.cs
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class CategoryAttribute : Attribute, ITraitAttribute
{
    public CategoryAttribute(Category category)
    {
        Category = category;
    }

    public Category Category { get; set; }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [new("Category", Category.ToString())];
    }
}
