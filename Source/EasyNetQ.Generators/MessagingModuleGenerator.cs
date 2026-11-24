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
///     [DeliveryMode]-annotated types, [MessageType] wire names and [assembly: EasyNetQMessages], then emits an
///     <c>{Assembly}.EasyNetQ.Generated.MessagingModule</c> that pre-registers every discovered type in the message
///     type registry (closed generics - AOT-safe, no runtime reflection), plus interceptors for AddEasyNetQ(...) call
///     sites that register the module automatically, composing modules from referenced assemblies via their
///     [assembly: EasyNetQModule] attributes. IConsume/IConsumeAsync implementations also become AutoSubscriber
///     registrations (closed generics plus their [AutoSubscriberConsumer]/[ForTopic]/[SubscriptionConfiguration]
///     values), so AutoSubscribe(...) needs no reflection.
///
///     Note: a JsonSerializerContext is deliberately NOT emitted. Roslyn generators cannot see each other's output,
///     so the System.Text.Json generator would never fill such a context. AOT users pass their own context to
///     UseSystemTextJson(context).
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MessagingModuleGenerator : IIncrementalGenerator
{
    private static readonly ImmutableHashSet<string> EasyNetQAssemblyNames = ImmutableHashSet.Create("EasyNetQ", "EasyNetQ.Core", "EasyNetQ.RabbitMQ");

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

        // (b2) the same implementations as AutoSubscriber registrations
        var autoSubscriberConsumers = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, ct) => HarvestAutoSubscriberConsumers(ctx, ct))
            .SelectMany(static (consumers, _) => consumers)
            .Collect();

        // (c) [Queue]/[Exchange]/[DeliveryMode]-annotated types
        var queueAnnotated = AttributeTargets(context, "EasyNetQ.QueueAttribute").Collect();
        var exchangeAnnotated = AttributeTargets(context, "EasyNetQ.ExchangeAttribute").Collect();
        var deliveryModeAnnotated = AttributeTargets(context, "EasyNetQ.DeliveryModeAttribute").Collect();

        // (c2) [MessageType("wire", Aliases = ...)]: explicit wire names, emitted as Register<T>(...) calls
        var wireNameMappings = context.SyntaxProvider.ForAttributeWithMetadataName(
                "EasyNetQ.MessageTypeAttribute",
                static (node, _) => node is ClassDeclarationSyntax or InterfaceDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax,
                static (ctx, _) => HarvestWireNameMapping(ctx))
            .Where(static mapping => mapping is not null)
            .Select(static (mapping, _) => mapping!)
            .Collect();

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

        // (g) AutoSubscribe(...) call sites: only then is a consumer the generated code cannot reach worth a warning
        var usesAutoSubscribe = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AutoSubscribe" } },
                static (ctx, ct) => ctx.SemanticModel.GetSymbolInfo(ctx.Node, ct).Symbol is IMethodSymbol { ContainingType.Name: "AutoSubscribeBuilderExtensions" } method
                    && EasyNetQAssemblyNames.Contains(method.ContainingType.ContainingAssembly?.Name ?? ""))
            .Where(static used => used)
            .Collect()
            .Select(static (calls, _) => !calls.IsEmpty);

        var everything = allTypes.Combine(compilationFacts).Combine(interceptions).Combine(wireNameMappings).Combine(autoSubscriberConsumers.Combine(usesAutoSubscribe));

        context.RegisterSourceOutput(everything, static (spc, source) =>
            Emit(spc, source.Left.Left.Left.Left, source.Left.Left.Left.Right, source.Left.Left.Right, source.Left.Right, source.Right.Left, source.Right.Right));
    }

    /// <summary>A [MessageType] registration; aliases are joined with '\n' to keep the record value-equatable.</summary>
    private sealed record WireNameMapping(string Type, string WireName, string Aliases);

    private static WireNameMapping? HarvestWireNameMapping(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol named || !IsEmittable(named)) return null;
        var attribute = ctx.Attributes[0];
        if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not string wireName) return null;

        var aliases = new List<string>();
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != "Aliases" || argument.Value.Kind != TypedConstantKind.Array) continue;
            foreach (var value in argument.Value.Values)
            {
                if (value.Value is string alias) aliases.Add(alias);
            }
        }

        return new WireNameMapping(named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), wireName, string.Join("\n", aliases));
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
            || !EasyNetQAssemblyNames.Contains(containingType.ContainingAssembly?.Name ?? "")
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
                && EasyNetQAssemblyNames.Contains(iface.ContainingAssembly?.Name ?? "")
                && iface.TypeArguments[0] is INamedTypeSymbol messageType
                && IsEmittable(messageType))
            {
                builder.Add(messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }
        return builder.ToImmutable();
    }

    /// <summary>
    ///     An AutoSubscriber registration: Factory is AutoSubscriberConsumer.Async or .Sync, the rest are C# expressions
    ///     for the attribute values ("null" when absent). Location is set only for consumers generated code cannot
    ///     reach, which get a warning instead of a registration.
    /// </summary>
    private sealed record AutoSubscriberRegistration(
        string ConsumerType,
        string MessageType,
        string Factory,
        string SubscriptionAttribute,
        string Topics,
        string SubscriptionConfiguration,
        Location? Inaccessible
    );

    private static readonly DiagnosticDescriptor InaccessibleConsumer = new(
        "ENQGEN001",
        "Consumer is not reachable from generated code",
        "'{0}' implements {1} but is not accessible from generated code, so AutoSubscribe(...) skips it; make it internal or public (and non-generic)",
        "EasyNetQ.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static ImmutableArray<AutoSubscriberRegistration> HarvestAutoSubscriberConsumers(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } classSymbol)
            return ImmutableArray<AutoSubscriberRegistration>.Empty;
        // a partial class is harvested once, from its first declaration
        if (classSymbol.DeclaringSyntaxReferences.Length > 1 && classSymbol.DeclaringSyntaxReferences[0].GetSyntax(ct) != ctx.Node)
            return ImmutableArray<AutoSubscriberRegistration>.Empty;

        var builder = ImmutableArray.CreateBuilder<AutoSubscriberRegistration>();
        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (iface is not { IsGenericType: true, Name: "IConsume" or "IConsumeAsync", TypeArguments.Length: 1 }
                || iface.ContainingNamespace?.ToDisplayString() != "EasyNetQ.AutoSubscribe"
                || !EasyNetQAssemblyNames.Contains(iface.ContainingAssembly?.Name ?? "")
                || iface.TypeArguments[0] is not INamedTypeSymbol messageType)
                continue;

            var consumerName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!IsEmittable(classSymbol) || !IsEmittable(messageType) || messageType.TypeKind is not (TypeKind.Class or TypeKind.Interface))
            {
                builder.Add(new AutoSubscriberRegistration(consumerName, iface.ToDisplayString(), "", "", "", "", ctx.Node.GetLocation()));
                continue;
            }

            var interfaceMethod = iface.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
            var method = MostDerivedOverride(classSymbol, interfaceMethod is null ? null : classSymbol.FindImplementationForInterfaceMember(interfaceMethod) as IMethodSymbol);

            var methodAttributes = new List<AttributeData>();
            for (var current = method; current is not null; current = current.OverriddenMethod)
                methodAttributes.AddRange(current.GetAttributes());
            var classAttributes = new List<AttributeData>();
            for (var current = (INamedTypeSymbol?)classSymbol; current is not null; current = current.BaseType)
                classAttributes.AddRange(current.GetAttributes());

            var subscription = methodAttributes.FirstOrDefault(a => IsAttribute(a, "AutoSubscriberConsumerAttribute"));
            var topics = methodAttributes.Where(a => IsAttribute(a, "ForTopicAttribute")).Select(a => AttributeExpression(a)).ToList();
            var configuration = methodAttributes.FirstOrDefault(a => IsAttribute(a, "SubscriptionConfigurationAttribute"))
                ?? classAttributes.FirstOrDefault(a => IsAttribute(a, "SubscriptionConfigurationAttribute"));

            builder.Add(new AutoSubscriberRegistration(
                consumerName,
                messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                iface.Name == "IConsumeAsync" ? "Async" : "Sync",
                subscription is null ? "null" : AttributeExpression(subscription),
                topics.Count == 0 ? "null" : "new string[] { " + string.Join(", ", topics.Select(t => t + ".Topic")) + " }",
                configuration is null ? "null" : AttributeExpression(configuration),
                null));
        }
        return builder.ToImmutable();
    }

    /// <summary>
    ///     Roslyn maps an interface member to the virtual method that implements it; the runtime (and so reflection's
    ///     GetInterfaceMap) dispatches to the most derived override of it.
    /// </summary>
    private static IMethodSymbol? MostDerivedOverride(INamedTypeSymbol classSymbol, IMethodSymbol? implementation)
    {
        if (implementation is null || !(implementation.IsVirtual || implementation.IsOverride || implementation.IsAbstract)) return implementation;
        for (var current = (INamedTypeSymbol?)classSymbol; current is not null; current = current.BaseType)
        {
            foreach (var candidate in current.GetMembers(implementation.Name).OfType<IMethodSymbol>())
            {
                for (var overridden = candidate; overridden is not null; overridden = overridden.OverriddenMethod)
                {
                    if (SymbolEqualityComparer.Default.Equals(overridden, implementation)) return candidate;
                }
            }
        }
        return implementation;
    }

    /// <summary>The attribute or a subclass of it, from EasyNetQ.AutoSubscribe</summary>
    private static bool IsAttribute(AttributeData attribute, string name)
    {
        for (var current = attribute.AttributeClass; current is not null; current = current.BaseType)
        {
            if (current.Name == name && current.ContainingNamespace?.ToDisplayString() == "EasyNetQ.AutoSubscribe")
                return true;
        }
        return false;
    }

    /// <summary>Recreates an attribute instance: <c>new T(args) { Named = value }</c></summary>
    private static string AttributeExpression(AttributeData attribute)
    {
        var expression = new StringBuilder("new ")
            .Append(attribute.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .Append('(')
            .Append(string.Join(", ", attribute.ConstructorArguments.Select(a => a.ToCSharpString())))
            .Append(')');
        if (attribute.NamedArguments.Length > 0)
            expression.Append(" { ").Append(string.Join(", ", attribute.NamedArguments.Select(a => a.Key + " = " + a.Value.ToCSharpString()))).Append(" }");
        return expression.ToString();
    }

    private sealed record CompilationFacts(
        string AssemblyName,
        bool ReferencesEasyNetQ,
        ImmutableArray<string> OptInTypes,
        ImmutableArray<string> ReferencedModules
    );

    private static CompilationFacts GetCompilationFacts(Compilation compilation, System.Threading.CancellationToken ct)
    {
        var referencesEasyNetQ = EasyNetQAssemblyNames.Contains(compilation.AssemblyName ?? "")
            || compilation.SourceModule.ReferencedAssemblySymbols.Any(a => EasyNetQAssemblyNames.Contains(a.Name));

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
            || !EasyNetQAssemblyNames.Contains(method.ContainingType.ContainingAssembly?.Name ?? ""))
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

    private static void Emit(
        SourceProductionContext spc,
        ImmutableArray<string> types,
        CompilationFacts facts,
        ImmutableArray<InterceptionSite> interceptSites,
        ImmutableArray<WireNameMapping> wireNameMappings,
        ImmutableArray<AutoSubscriberRegistration> autoSubscriberRegistrations,
        bool usesAutoSubscribe
    )
    {
        if (!facts.ReferencesEasyNetQ || EasyNetQAssemblyNames.Contains(facts.AssemblyName)) return;

        // the reflection AutoSubscriber still reaches private consumers, so they only matter to AutoSubscribe(...)
        foreach (var inaccessible in autoSubscriberRegistrations.Where(r => usesAutoSubscribe && r.Inaccessible is not null))
            spc.ReportDiagnostic(Diagnostic.Create(InaccessibleConsumer, inaccessible.Inaccessible, inaccessible.ConsumerType.Replace("global::", ""), inaccessible.MessageType));
        var consumers = autoSubscriberRegistrations
            .Where(r => r.Inaccessible is null)
            .Distinct()
            .OrderBy(r => r.ConsumerType, StringComparer.Ordinal)
            .ThenBy(r => r.MessageType, StringComparer.Ordinal)
            .ToList();

        var mappings = wireNameMappings.OrderBy(m => m.Type, StringComparer.Ordinal).ToList();
        var mappedTypes = new HashSet<string>(mappings.Select(m => m.Type), StringComparer.Ordinal);
        var messageTypes = types.Concat(facts.OptInTypes)
            .Where(t => !mappedTypes.Contains(t))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();
        var hasModule = messageTypes.Count > 0 || mappings.Count > 0 || consumers.Count > 0;
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
            if (consumers.Count > 0)
            {
                source.AppendLine("            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(");
                source.AppendLine("                services,");
                source.AppendLine("                global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<global::EasyNetQ.AutoSubscribe.IAutoSubscriberConsumerSource>(AutoSubscriberConsumerSource.Instance));");
            }
            source.AppendLine("        }");
            source.AppendLine();
            source.AppendLine("        private sealed class RegistryInitializer : global::EasyNetQ.IMessageTypeRegistryInitializer");
            source.AppendLine("        {");
            source.AppendLine("            public static readonly RegistryInitializer Instance = new();");
            source.AppendLine();
            source.AppendLine("            public void Initialize(global::EasyNetQ.IMessageTypeRegistry registry)");
            source.AppendLine("            {");
            foreach (var mapping in mappings)
            {
                var aliases = mapping.Aliases.Length == 0
                    ? "null"
                    : "new string[] { " + string.Join(", ", mapping.Aliases.Split('\n').Select(a => SymbolDisplay.FormatLiteral(a, true))) + " }";
                source.AppendLine($"                registry.Register<{mapping.Type}>({SymbolDisplay.FormatLiteral(mapping.WireName, true)}, {aliases});");
            }
            foreach (var messageType in messageTypes)
                source.AppendLine($"                registry.GetOrAdd<{messageType}>();");
            source.AppendLine("            }");
            source.AppendLine("        }");
            if (consumers.Count > 0)
            {
                source.AppendLine();
                source.AppendLine("        private sealed class AutoSubscriberConsumerSource : global::EasyNetQ.AutoSubscribe.IAutoSubscriberConsumerSource");
                source.AppendLine("        {");
                source.AppendLine("            public static readonly AutoSubscriberConsumerSource Instance = new();");
                source.AppendLine();
                source.AppendLine("            public global::System.Collections.Generic.IReadOnlyList<global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumer> Consumers => AutoSubscriberConsumers.All;");
                source.AppendLine("        }");
            }
            source.AppendLine("    }");
            source.AppendLine();
        }

        if (consumers.Count > 0)
        {
            source.AppendLine("    /// <summary>This assembly's IConsume/IConsumeAsync implementations, for AutoSubscriber.SubscribeAsync(...)</summary>");
            source.AppendLine("    public static class AutoSubscriberConsumers");
            source.AppendLine("    {");
            source.AppendLine("        /// <summary>Every consumer, with the values of its subscription attributes</summary>");
            source.AppendLine("        public static global::System.Collections.Generic.IReadOnlyList<global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumer> All { get; } = new global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumer[]");
            source.AppendLine("        {");
            foreach (var consumer in consumers)
                source.AppendLine($"            global::EasyNetQ.AutoSubscribe.AutoSubscriberConsumer.{consumer.Factory}<{consumer.MessageType}, {consumer.ConsumerType}>({consumer.SubscriptionAttribute}, {consumer.Topics}, {consumer.SubscriptionConfiguration}),");
            source.AppendLine("        };");
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
