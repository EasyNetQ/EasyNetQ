using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EasyNetQ.Generators;

/// <summary>
///     Harvests message types from EasyNetQ call sites, IConsume/IConsumeAsync implementations, [Queue]/[Exchange]/
///     [DeliveryMode]-annotated types and [assembly: EasyNetQMessages], then emits an
///     <c>{Assembly}.EasyNetQ.Generated.MessagingModule</c> that pre-registers every discovered type in the message
///     type registry (closed generics - AOT-safe, no runtime reflection), plus interceptors for AddEasyNetQ(...) call
///     sites that register the module automatically, composing modules from referenced assemblies via their
///     [assembly: EasyNetQModule] attributes.
///
///     Note: a JsonSerializerContext is deliberately NOT emitted. Roslyn generators cannot see each other's output,
///     so the System.Text.Json generator would never fill such a context. AOT users pass their own context to
///     UseSystemTextJson(context).
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MessagingModuleGenerator : IIncrementalGenerator
{
    private const string EasyNetQAssemblyName = "EasyNetQ";

    /// <summary>Containing types (namespace EasyNetQ, assembly EasyNetQ) whose generic-method type arguments are message types.</summary>
    private static readonly ImmutableHashSet<string> HarvestedContainingTypes = ImmutableHashSet.Create(
        "IPubSub", "PubSubExtensions",
        "IRpc", "RpcExtensions",
        "ISendReceive", "SendReceiveExtensions",
        "IScheduler", "SchedulerExtensions",
        "IAdvancedBus", "AdvancedBusExtensions",
        "ConsumeConfigurationExtensions",
        "IReceiveRegistration", "ReceiveRegistrationExtensions",
        "IMessageTypeRegistry",
        "HandlerTable", "HandlerCollection"
    );

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // (a) closed type arguments at generic EasyNetQ call sites
        var callSiteTypes = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax,
                static (ctx, ct) => HarvestCallSite(ctx, ct))
            .SelectMany(static (types, _) => types);

        // (b) IConsume<T> / IConsumeAsync<T> implementations
        var consumerTypes = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, ct) => HarvestConsumerImplementations(ctx, ct))
            .SelectMany(static (types, _) => types);

        // (c) [Queue]/[Exchange]/[DeliveryMode]-annotated types
        var queueAnnotated = AttributeTargets(context, "EasyNetQ.QueueAttribute").Collect();
        var exchangeAnnotated = AttributeTargets(context, "EasyNetQ.ExchangeAttribute").Collect();
        var deliveryModeAnnotated = AttributeTargets(context, "EasyNetQ.DeliveryModeAttribute").Collect();

        // (d) [assembly: EasyNetQMessages(typeof(...))] opt-ins + (e) referenced modules + assembly identity
        var compilationFacts = context.CompilationProvider.Select(static (compilation, ct) => GetCompilationFacts(compilation, ct));

        // (f) AddEasyNetQ call sites to intercept
        var interceptions = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddEasyNetQ" } },
                static (ctx, ct) => HarvestAddEasyNetQ(ctx, ct))
            .Where(static site => site is not null)
            .Select(static (site, _) => site!)
            .Collect();

        var allTypes = callSiteTypes.Collect()
            .Combine(consumerTypes.Collect())
            .Combine(queueAnnotated)
            .Combine(exchangeAnnotated)
            .Combine(deliveryModeAnnotated)
            .Select(static (t, _) => t.Left.Left.Left.Left
                .Concat(t.Left.Left.Left.Right)
                .Concat(t.Left.Left.Right)
                .Concat(t.Left.Right)
                .Concat(t.Right)
                .ToImmutableArray());

        var everything = allTypes.Combine(compilationFacts).Combine(interceptions);

        context.RegisterSourceOutput(everything, static (spc, source) =>
            Emit(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static IncrementalValuesProvider<string> AttributeTargets(IncrementalGeneratorInitializationContext context, string attributeMetadataName)
        => context.SyntaxProvider.ForAttributeWithMetadataName(
                attributeMetadataName,
                static (node, _) => node is ClassDeclarationSyntax or InterfaceDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, _) => ctx.TargetSymbol is INamedTypeSymbol named && IsEmittable(named)
                    ? named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : null)
            .Where(static name => name is not null)
            .Select(static (name, _) => name!);

    private static ImmutableArray<string> HarvestCallSite(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.SemanticModel.GetSymbolInfo(ctx.Node, ct).Symbol is not IMethodSymbol { IsGenericMethod: true } method)
            return ImmutableArray<string>.Empty;

        var containingType = method.ContainingType;
        if (containingType is null
            || containingType.ContainingAssembly?.Name != EasyNetQAssemblyName
            || containingType.ContainingNamespace?.ToDisplayString() != "EasyNetQ"
            || !HarvestedContainingTypes.Contains(containingType.Name))
            return ImmutableArray<string>.Empty;

        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var typeArgument in method.TypeArguments)
        {
            if (typeArgument is INamedTypeSymbol named && IsEmittable(named))
                builder.Add(named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
        return builder.ToImmutable();
    }

    private static ImmutableArray<string> HarvestConsumerImplementations(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } classSymbol)
            return ImmutableArray<string>.Empty;

        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (iface is { IsGenericType: true, Name: "IConsume" or "IConsumeAsync", TypeArguments.Length: 1 }
                && iface.ContainingNamespace?.ToDisplayString() == "EasyNetQ.AutoSubscribe"
                && iface.ContainingAssembly?.Name == EasyNetQAssemblyName
                && iface.TypeArguments[0] is INamedTypeSymbol messageType
                && IsEmittable(messageType))
            {
                builder.Add(messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }
        return builder.ToImmutable();
    }

    private sealed record CompilationFacts(
        string AssemblyName,
        bool ReferencesEasyNetQ,
        ImmutableArray<string> OptInTypes,
        ImmutableArray<string> ReferencedModules
    );

    private static CompilationFacts GetCompilationFacts(Compilation compilation, System.Threading.CancellationToken ct)
    {
        var referencesEasyNetQ = compilation.AssemblyName == EasyNetQAssemblyName
            || compilation.SourceModule.ReferencedAssemblySymbols.Any(a => a.Name == EasyNetQAssemblyName);

        var optIns = ImmutableArray.CreateBuilder<string>();
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "EasyNetQ.EasyNetQMessagesAttribute") continue;
            foreach (var arg in attribute.ConstructorArguments.SelectMany(Flatten))
            {
                if (arg.Value is INamedTypeSymbol named && IsEmittable(named))
                    optIns.Add(named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        var referencedModules = ImmutableArray.CreateBuilder<string>();
        foreach (var referenced in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var attribute in referenced.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != "EasyNetQ.EasyNetQModuleAttribute") continue;
                if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol moduleType)
                    referencedModules.Add(moduleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        return new CompilationFacts(
            compilation.AssemblyName ?? "Assembly",
            referencesEasyNetQ,
            optIns.ToImmutable(),
            referencedModules.ToImmutable()
        );

        static IEnumerable<TypedConstant> Flatten(TypedConstant constant)
            => constant.Kind == TypedConstantKind.Array ? constant.Values.SelectMany(Flatten) : new[] { constant };
    }

    private sealed record InterceptionSite(string AttributeSyntax, string ParameterList, string ArgumentList);

    private static InterceptionSite? HarvestAddEasyNetQ(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol method
            || method.Name != "AddEasyNetQ"
            || method.ContainingType?.Name != "RabbitHutch"
            || method.ContainingType.ContainingAssembly?.Name != EasyNetQAssemblyName)
            return null;

#pragma warning disable RSEXPERIMENTAL002
        var location = ctx.SemanticModel.GetInterceptableLocation(invocation, ct);
#pragma warning restore RSEXPERIMENTAL002
        if (location is null) return null;

        // method is the reduced extension form: parameters exclude the receiver
        var parameters = new StringBuilder("this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services");
        var arguments = new StringBuilder("services");
        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];
            parameters.Append(", ").Append(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(" arg").Append(i);
            arguments.Append(", arg").Append(i);
        }

#pragma warning disable RSEXPERIMENTAL002
        return new InterceptionSite(location.GetInterceptsLocationAttributeSyntax(), parameters.ToString(), arguments.ToString());
#pragma warning restore RSEXPERIMENTAL002
    }

    private static bool IsEmittable(INamedTypeSymbol type)
    {
        if (type.IsUnboundGenericType || type.TypeKind is TypeKind.Error or TypeKind.TypeParameter) return false;
        if (type.IsRefLikeType || type.SpecialType == SpecialType.System_Void) return false;
        if (type.IsFileLocal) return false;

        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.ProtectedAndInternal or Accessibility.Protected)
                return false;
        }

        foreach (var typeArgument in type.TypeArguments)
        {
            if (typeArgument is not INamedTypeSymbol namedArgument || !IsEmittable(namedArgument)) return false;
        }

        return true;
    }

    private static string SanitizeIdentifier(string assemblyName)
    {
        var builder = new StringBuilder(assemblyName.Length);
        foreach (var c in assemblyName)
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        if (builder.Length == 0 || char.IsDigit(builder[0])) builder.Insert(0, '_');
        return builder.ToString();
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<string> types, CompilationFacts facts, ImmutableArray<InterceptionSite> interceptSites)
    {
        if (!facts.ReferencesEasyNetQ || facts.AssemblyName == EasyNetQAssemblyName) return;

        var messageTypes = types.Concat(facts.OptInTypes).Distinct(StringComparer.Ordinal).OrderBy(t => t, StringComparer.Ordinal).ToList();
        var hasModule = messageTypes.Count > 0;
        if (!hasModule && interceptSites.IsEmpty && facts.ReferencedModules.IsEmpty) return;

        var ns = $"EasyNetQ.Generated.{SanitizeIdentifier(facts.AssemblyName)}";
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated by EasyNetQ.Generators />");
        source.AppendLine("#nullable enable");
        source.AppendLine();

        if (hasModule)
        {
            source.AppendLine($"[assembly: global::EasyNetQ.EasyNetQModule(typeof(global::{ns}.MessagingModule))]");
            source.AppendLine();
        }

        source.AppendLine($"namespace {ns}");
        source.AppendLine("{");

        if (hasModule)
        {
            source.AppendLine("    /// <summary>Compile-time-generated EasyNetQ registrations for this assembly.</summary>");
            source.AppendLine("    public sealed class MessagingModule : global::EasyNetQ.IEasyNetQModule");
            source.AppendLine("    {");
            source.AppendLine("        /// <inheritdoc />");
            source.AppendLine("        public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
            source.AppendLine("        {");
            source.AppendLine("            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(");
            source.AppendLine("                services,");
            source.AppendLine("                global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<global::EasyNetQ.IMessageTypeRegistryInitializer>(RegistryInitializer.Instance));");
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        private sealed class RegistryInitializer : global::EasyNetQ.IMessageTypeRegistryInitializer");
            source.AppendLine("        {");
            source.AppendLine("            public static readonly RegistryInitializer Instance = new();");
            source.AppendLine();
            source.AppendLine("            public void Initialize(global::EasyNetQ.IMessageTypeRegistry registry)");
            source.AppendLine("            {");
            foreach (var messageType in messageTypes)
                source.AppendLine($"                registry.GetOrAdd<{messageType}>();");
            source.AppendLine("            }");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine();
        }

        // Manual fallback + shared registration helper
        source.AppendLine("    /// <summary>Registers this assembly's generated module and every referenced assembly's module.</summary>");
        source.AppendLine("    public static class GeneratedModules");
        source.AppendLine("    {");
        source.AppendLine("        /// <summary>Adds all generated modules to the builder. Idempotent.</summary>");
        source.AppendLine("        public static global::EasyNetQ.IEasyNetQBuilder AddGeneratedModules(this global::EasyNetQ.IEasyNetQBuilder builder)");
        source.AppendLine("        {");
        if (hasModule)
            source.AppendLine("            global::EasyNetQ.EasyNetQBuilderModuleExtensions.AddModule(builder, new MessagingModule());");
        foreach (var module in facts.ReferencedModules.Distinct(StringComparer.Ordinal).OrderBy(m => m, StringComparer.Ordinal))
            source.AppendLine($"            global::EasyNetQ.EasyNetQBuilderModuleExtensions.AddModule(builder, new {module}());");
        source.AppendLine("            return builder;");
        source.AppendLine("        }");
        source.AppendLine("    }");

        if (!interceptSites.IsEmpty)
        {
            source.AppendLine();
            source.AppendLine("    /// <summary>Intercepts AddEasyNetQ(...) call sites to register generated modules automatically.</summary>");
            source.AppendLine("    public static class AddEasyNetQInterceptors");
            source.AppendLine("    {");
            var index = 0;
            foreach (var site in interceptSites.Distinct())
            {
                source.AppendLine($"        {site.AttributeSyntax}");
                source.AppendLine($"        public static global::EasyNetQ.IEasyNetQBuilder AddEasyNetQ{index}({site.ParameterList})");
                source.AppendLine("        {");
                source.AppendLine($"            var builder = global::EasyNetQ.RabbitHutch.AddEasyNetQ({site.ArgumentList});");
                source.AppendLine("            return GeneratedModules.AddGeneratedModules(builder);");
                source.AppendLine("        }");
                source.AppendLine();
                index++;
            }
            source.AppendLine("    }");
        }

        source.AppendLine("}");

        if (!interceptSites.IsEmpty)
        {
            source.AppendLine();
            source.AppendLine("namespace System.Runtime.CompilerServices");
            source.AppendLine("{");
            source.AppendLine("    // Polyfill so [InterceptsLocation] compiles on every TFM; 'file' scope keeps it private to this file");
            source.AppendLine("    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
            source.AppendLine("    file sealed class InterceptsLocationAttribute : global::System.Attribute");
            source.AppendLine("    {");
            source.AppendLine("        public InterceptsLocationAttribute(int version, string data)");
            source.AppendLine("        {");
            source.AppendLine("            _ = version;");
            source.AppendLine("            _ = data;");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");
        }

        spc.AddSource("EasyNetQ.MessagingModule.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }
}
