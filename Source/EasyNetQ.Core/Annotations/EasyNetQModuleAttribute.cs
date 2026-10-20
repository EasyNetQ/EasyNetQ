namespace EasyNetQ;

/// <summary>
///     Marks an assembly as carrying a generated (or hand-written) <see cref="IEasyNetQModule" />. The EasyNetQ
///     source generator reads this attribute from referenced assemblies at compile time to compose modules across
///     assembly boundaries - no runtime reflection is involved.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class EasyNetQModuleAttribute : Attribute
{
    /// <summary>
    ///     Declares that <paramref name="moduleType" /> is an <see cref="IEasyNetQModule" /> for this assembly.
    /// </summary>
    public EasyNetQModuleAttribute(Type moduleType)
    {
        ModuleType = moduleType;
    }

    /// <summary>
    ///     The module type; must implement <see cref="IEasyNetQModule" /> and have a public parameterless constructor.
    /// </summary>
    public Type ModuleType { get; }
}
