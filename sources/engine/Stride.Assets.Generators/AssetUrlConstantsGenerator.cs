// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stride.Assets.Generators;

/// <summary>
/// Generates typed <c>UrlReference</c> constants for the project's authored assets, exposed as
/// the property StrideAssetPaths by Stride.AssetConstants.targets, so game code can write
/// <c>Assets.Materials.Ground</c> instead of a URL string.
/// </summary>
[Generator]
public class AssetUrlConstantsGenerator : IIncrementalGenerator
{
    private const string DefaultClassName = "Assets";
    private const string GeneratedFileName = "StrideAssetConstants.g.cs";

    private const string UrlReferenceTypeName = "global::Stride.Core.Serialization.UrlReference";
    private const string AssetContentTypeAttributeFullName = "Stride.Core.Assets.AssetContentTypeAttribute";
    private const string AssetDescriptionAttributeFullName = "Stride.Core.Assets.AssetDescriptionAttribute";
    private const string StrideCoreAssetsAssemblyName = "Stride.Core.Assets";
    private const string AssetFileExtensionAttributeFullName = "Stride.Core.Serialization.AssetFileExtensionAttribute";

    private static readonly DiagnosticDescriptor IdentifierCollision = new(
        "STRDIAG012",
        "Asset constant identifier collision",
        "The asset '{0}' maps to identifier '{1}' which is already taken; it was emitted as '{2}'",
        "Stride",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ClassNameConflict = new(
        "STRDIAG013",
        "Asset constants class name conflicts with a namespace",
        "The asset constants class '{0}.{1}' conflicts with an existing namespace; set <StrideAssetConstantsClassName> (e.g. '{2}') to generate under a different name",
        "Stride",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UndeclaredFormerEngineExtension = new(
        "STRDIAG015",
        "Asset constants of a former engine asset type are untyped",
        "The '{0}' asset constants are untyped: this asset type moved out of the engine; reference the {1} package to get typed constants",
        "Stride",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // The asset extensions the engine held before 4.5, with the package that declares them since
    private static readonly Dictionary<string, string> FormerEngineExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".sdhull"] = "Stride.BepuPhysics",
        [".sdvid"] = "Stride.Video",
        [".sdphy"] = "Stride.Physics",
        [".sdhmap"] = "Stride.Physics",
        [".sdnavmesh"] = "Stride.Navigation",
        [".sdss4s"] = "Stride.SpriteStudio.Runtime",
        [".sdss4a"] = "Stride.SpriteStudio.Runtime",
        [".sdsnd"] = "Stride.Audio",
        [".sduipage"] = "Stride.UI",
        [".sduilib"] = "Stride.UI",
    };

    internal sealed record AssetEntry(string RelativePath, string Extension);

    internal sealed record Config(string UrlNamespace, string ClassName, string Namespace);

    internal sealed record ExtensionType(string Extension, string TypeName);

    internal sealed record ConflictInfo(bool HasConflict, string Namespace, string ClassName, string Suggestion)
    {
        public static readonly ConflictInfo None = new(false, "", "", "");
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assets = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
        {
            if (!options.GlobalOptions.TryGetValue("build_property.StrideAssetPaths", out var encodedPaths) || string.IsNullOrEmpty(encodedPaths))
                return [];

            var result = new List<AssetEntry>();
            foreach (var escapedPath in encodedPaths.Split(['|'], StringSplitOptions.RemoveEmptyEntries))
            {
                var path = Uri.UnescapeDataString(escapedPath);
                var extension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(extension))
                    continue;
                var pathWithoutExtension = path[..^extension.Length];
                result.Add(new AssetEntry(pathWithoutExtension.Replace('\\', '/'), extension));
            }
            return result;
        }).WithComparer(SequentialListComparer<AssetEntry>.Instance);

        var config = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => ReadConfig(provider));

        // Keep the compilation out of the source-output stage: it changes every keystroke. The
        // symbol-dependent parts (type table, name conflict) run in their own stages returning
        // value-equatable results, so the emit stays cached when assets and config are unchanged.
        // hasAssets keeps the symbol work off projects that only reference Stride.Engine.
        var hasAssets = assets.Select(static (entries, _) => entries.Count > 0);

        var typeTable = context.CompilationProvider.Combine(hasAssets)
            .Select(static (data, cancellationToken) => data.Right
                ? ResolveExtensionTypes(data.Left, cancellationToken)
                : [])
            .WithComparer(SequentialListComparer<ExtensionType>.Instance);

        var conflict = context.CompilationProvider.Combine(config).Combine(hasAssets)
            .Select(static (data, _) => data.Right
                ? ResolveConflict(data.Left.Left, data.Left.Right)
                : ConflictInfo.None);

        var input = assets.Combine(typeTable).Combine(config).Combine(conflict);
        context.RegisterSourceOutput(input, static (productionContext, data) =>
            Emit(productionContext, data.Left.Left.Left, data.Left.Left.Right, data.Left.Right, data.Right));
    }

    private static Config ReadConfig(AnalyzerConfigOptionsProvider provider)
    {
        provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        provider.GlobalOptions.TryGetValue("build_property.StrideAssetUrlNamespace", out var urlNamespace);
        provider.GlobalOptions.TryGetValue("build_property.StrideAssetConstantsClassName", out var className);
        provider.GlobalOptions.TryGetValue("build_property.StrideAssetConstantsNamespace", out var constantsNamespace);
        return new Config(
            urlNamespace ?? "",
            string.IsNullOrEmpty(className) ? DefaultClassName : SanitizeIdentifier(className!),
            (string.IsNullOrEmpty(constantsNamespace) ? rootNamespace : constantsNamespace) ?? "");
    }

    // Maps each asset extension to its runtime type from declarations and visible asset types.
    // Sorted so the array compares equal.
    private static List<ExtensionType> ResolveExtensionTypes(Compilation compilation, CancellationToken cancellationToken)
    {
        var extensionTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var extensionAttribute = compilation.GetTypeByMetadataName(AssetFileExtensionAttributeFullName);
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols.Concat([compilation.Assembly]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var pair in ReadDeclaredExtensions(assembly, extensionAttribute))
                extensionTypes[pair.Key] = pair.Value;
        }
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols.Concat([compilation.Assembly]))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (assembly.Modules.Any(module => module.ReferencedAssemblies.Any(identity => identity.Name == StrideCoreAssetsAssemblyName)))
                CollectAssetTypes(assembly.GlobalNamespace, extensionTypes);
        }
        return extensionTypes
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ExtensionType(pair.Key, pair.Value))
            .ToList();
    }

    private static ConflictInfo ResolveConflict(Compilation compilation, Config config)
        => HasClassNameConflict(compilation, config, out var suggestion)
            ? new ConflictInfo(true, config.Namespace, config.ClassName, suggestion)
            : ConflictInfo.None;

    private static void Emit(SourceProductionContext context, List<AssetEntry> assets, List<ExtensionType> typeTable, Config config, ConflictInfo conflict)
    {
        // Extension -> runtime type (empty = untyped); a file with an unknown extension gets no constant
        var extensionTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in typeTable)
            extensionTypes[entry.Extension] = entry.TypeName;

        // A former engine extension the game's references do not declare keeps an untyped constant, as before
        foreach (var extension in assets.Select(entry => entry.Extension).Where(FormerEngineExtensions.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (extensionTypes.ContainsKey(extension))
                continue;
            extensionTypes[extension] = "";
            context.ReportDiagnostic(Diagnostic.Create(UndeclaredFormerEngineExtension, Location.None, extension, FormerEngineExtensions[extension]));
        }

        var entries = assets
            .Where(entry => extensionTypes.ContainsKey(entry.Extension))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
            .ToList();
        if (entries.Count == 0)
            return;

        if (conflict.HasConflict)
        {
            context.ReportDiagnostic(Diagnostic.Create(ClassNameConflict, Location.None, conflict.Namespace, conflict.ClassName, conflict.Suggestion));
            return;
        }

        var root = new Node();
        foreach (var entry in entries)
        {
            var segments = entry.RelativePath.Split('/');
            var node = root;
            for (int i = 0; i < segments.Length - 1; i++)
                node = node.GetOrAddChild(segments[i]);
            node.Leaves.Add((segments[segments.Length - 1], entry));
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        var indent = 0;
        if (config.Namespace.Length > 0)
        {
            builder.Append("namespace ").AppendLine(SanitizeNamespace(config.Namespace));
            builder.AppendLine("{");
            indent++;
        }
        EmitClass(context, builder, root, config.ClassName, config, extensionTypes, indent);
        if (config.Namespace.Length > 0)
            builder.AppendLine("}");

        context.AddSource(GeneratedFileName, builder.ToString());
    }

    private static bool HasClassNameConflict(Compilation compilation, Config config, out string suggestion)
    {
        suggestion = "";
        // Only source-declared namespaces conflict outright (CS0434); referenced-assembly
        // namespaces of the same name are legal until a use site is ambiguous.
        var ns = compilation.Assembly.GlobalNamespace;
        if (config.Namespace.Length > 0)
        {
            foreach (var part in config.Namespace.Split('.'))
            {
                ns = ns.GetNamespaceMembers().FirstOrDefault(member => member.Name == part);
                if (ns is null)
                    return false;
            }
        }
        if (!ns.GetNamespaceMembers().Any(member => member.Name == config.ClassName))
            return false;
        suggestion = SanitizeIdentifier(compilation.AssemblyName?.Replace(".", "") + config.ClassName);
        return true;
    }

    /// <summary>
    /// The [assembly: AssetFileExtension] declarations of one assembly.
    /// A runtime package declares the extensions of its Assets package, which the game does not reference.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> ReadDeclaredExtensions(IAssemblySymbol assembly, INamedTypeSymbol? attributeType)
    {
        if (attributeType is null)
            yield break;
        foreach (var attribute in assembly.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)
                && attribute.ConstructorArguments.Length == 2
                && attribute.ConstructorArguments[0].Value is string extension
                && attribute.ConstructorArguments[1].Value is INamedTypeSymbol type)
                yield return new KeyValuePair<string, string>(extension, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
    }

    /// <summary>
    /// Compilation-visible asset types (e.g. a ProjectReference'd plugin): [AssetContentType]
    /// gives the runtime type, [AssetDescription] its file extension(s). Only assemblies that
    /// reference Stride.Core.Assets can declare asset types, so anything else is skipped whole.
    /// </summary>
    private static void CollectAssetTypes(INamespaceSymbol ns, Dictionary<string, string> result)
    {
        foreach (var member in ns.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol nested:
                    CollectAssetTypes(nested, result);
                    break;
                case INamedTypeSymbol { TypeKind: TypeKind.Class } type:
                    string? fileExtensions = null;
                    INamedTypeSymbol? contentType = null;
                    foreach (var attribute in type.GetAttributes())
                    {
                        var attributeName = attribute.AttributeClass?.ToDisplayString();
                        if (attributeName == AssetContentTypeAttributeFullName)
                            contentType = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : null;
                        else if (attributeName == AssetDescriptionAttributeFullName && attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string extensions)
                            fileExtensions = extensions;
                    }
                    if (fileExtensions is null || contentType is not { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public })
                        break;
                    var typeName = contentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    foreach (var extension in fileExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (extension.StartsWith(".", StringComparison.Ordinal))
                            result[extension] = typeName;
                    }
                    break;
            }
        }
    }

    private sealed class Node
    {
        public SortedDictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);
        public List<(string Name, AssetEntry Entry)> Leaves { get; } = [];

        public Node GetOrAddChild(string name)
        {
            if (!Children.TryGetValue(name, out var child))
                Children.Add(name, child = new Node());
            return child;
        }
    }

    private static void EmitClass(SourceProductionContext context, StringBuilder builder, Node node, string className, Config config, Dictionary<string, string> extensionTypes, int indent)
    {
        var pad = new string(' ', indent * 4);
        builder.Append(pad).Append("internal static partial class ").AppendLine(className);
        builder.Append(pad).AppendLine("{");

        // Members and nested classes share one declaration space, and none may shadow the container
        var usedNames = new HashSet<string>(StringComparer.Ordinal) { className };

        var children = new List<(string Identifier, Node Child)>();
        foreach (var pair in node.Children)
            children.Add((TakeIdentifier(context, usedNames, pair.Key, pair.Key), pair.Value));
        foreach (var (name, entry) in node.Leaves)
        {
            var identifier = TakeIdentifier(context, usedNames, name, entry.RelativePath);
            var url = config.UrlNamespace.Length > 0 ? $"/{config.UrlNamespace}/{entry.RelativePath}" : entry.RelativePath;
            var type = extensionTypes.TryGetValue(entry.Extension, out var contentType) && contentType.Length > 0 ? $"{UrlReferenceTypeName}<{contentType}>" : UrlReferenceTypeName;
            builder.Append(pad).Append("    public static readonly ").Append(type).Append(' ').Append(identifier)
                .Append(" = new ").Append(type).Append("(\"").Append(url.Replace("\\", "\\\\").Replace("\"", "\\\"")).AppendLine("\");");
        }
        foreach (var (identifier, child) in children)
            EmitClass(context, builder, child, identifier, config, extensionTypes, indent + 1);

        builder.Append(pad).AppendLine("}");
    }

    private static string TakeIdentifier(SourceProductionContext context, HashSet<string> usedNames, string name, string sourcePath)
    {
        var identifier = SanitizeIdentifier(name);
        if (!usedNames.Add(identifier))
        {
            var index = 1;
            string candidate;
            do
            {
                candidate = $"{identifier}_{index++}";
            }
            while (!usedNames.Add(candidate));
            context.ReportDiagnostic(Diagnostic.Create(IdentifierCollision, Location.None, sourcePath, identifier, candidate));
            identifier = candidate;
        }
        return identifier;
    }

    private static string SanitizeIdentifier(string name)
    {
        if (name.Length == 0)
            return "_";
        var builder = new StringBuilder(name.Length + 1);
        if (SyntaxFacts.IsIdentifierStartCharacter(name[0]))
            builder.Append(name[0]);
        else if (SyntaxFacts.IsIdentifierPartCharacter(name[0]))
            builder.Append('_').Append(name[0]);
        else
            builder.Append('_');
        for (int i = 1; i < name.Length; i++)
            builder.Append(SyntaxFacts.IsIdentifierPartCharacter(name[i]) ? name[i] : '_');
        var identifier = builder.ToString();
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
    }

    private static string SanitizeNamespace(string ns)
        => string.Join(".", ns.Split('.').Select(SanitizeIdentifier));
}
