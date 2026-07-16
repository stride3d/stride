// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stride.Assets.Generators;

/// <summary>
/// Generates typed <c>UrlReference</c> constants for the project's authored assets, exposed as
/// AdditionalFiles by Stride.AssetConstants.targets, so game code can write
/// <c>Assets.Materials.Ground</c> instead of a URL string.
/// </summary>
[Generator]
public class AssetUrlConstantsGenerator : IIncrementalGenerator
{
    private const string DefaultClassName = "Assets";
    private const string GeneratedFileName = "StrideAssetConstants.g.cs";

    private const string AssetFolderMetadataKey = "build_metadata.AdditionalFiles.StrideAssetFolder";
    private const string AssetContentTypeMapMetadataKey = "build_metadata.AdditionalFiles.StrideAssetContentTypeMap";
    private const string UrlReferenceTypeName = "global::Stride.Core.Serialization.UrlReference";
    private const string AssetContentTypeAttributeFullName = "Stride.Core.Assets.AssetContentTypeAttribute";
    private const string DataContractAttributeFullName = "Stride.Core.DataContractAttribute";
    private const string StrideCoreAssetsAssemblyName = "Stride.Core.Assets";

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

    internal sealed record AssetEntry(string RelativePath, string Tag);

    internal sealed record MapEntry(string Tag, string TypeName);

    internal sealed record Config(string UrlNamespace, string ClassName, string Namespace);

    internal sealed record TagType(string Tag, string TypeName);

    internal sealed record ConflictInfo(bool HasConflict, string Namespace, string ClassName, string Suggestion)
    {
        public static readonly ConflictInfo None = new(false, "", "", "");
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var filesWithOptions = context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider);

        var assets = filesWithOptions
            .Select(static (pair, cancellationToken) => ReadAssetEntry(pair.Left, pair.Right.GetOptions(pair.Left), cancellationToken))
            .Where(static entry => entry is not null)
            .Select(static (entry, _) => entry!)
            .Collect();

        var mapEntries = filesWithOptions
            .SelectMany(static (pair, cancellationToken) => ReadMapEntries(pair.Left, pair.Right.GetOptions(pair.Left), cancellationToken))
            .Collect();

        var config = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => ReadConfig(provider));

        // Keep the compilation out of the source-output stage: it changes every keystroke. The
        // symbol-dependent parts (type table, name conflict) run in their own stages returning
        // value-equatable results, so the emit stays cached when assets and config are unchanged.
        // hasAssets keeps the symbol work off projects that only reference Stride.Engine.
        var hasAssets = assets.Select(static (entries, _) => !entries.IsEmpty);

        var typeTable = context.CompilationProvider.Combine(mapEntries).Combine(hasAssets)
            .Select(static (data, cancellationToken) => data.Right
                ? ResolveTagTypes(data.Left.Left, data.Left.Right, cancellationToken)
                : ImmutableArray<TagType>.Empty)
            .WithComparer(TagTableComparer.Instance);

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

    // Resolves each asset tag to its runtime type (map files first, compilation symbols override),
    // sorted so equal resolutions produce an equal array.
    private static ImmutableArray<TagType> ResolveTagTypes(Compilation compilation, ImmutableArray<MapEntry> mapEntries, CancellationToken cancellationToken)
    {
        var tagTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapEntry in mapEntries.OrderBy(entry => entry.Tag, StringComparer.Ordinal).ThenBy(entry => entry.TypeName, StringComparer.Ordinal))
        {
            if (tagTypes.ContainsKey(mapEntry.Tag))
                continue;
            if (compilation.GetTypeByMetadataName(mapEntry.TypeName) is { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public } type)
                tagTypes[mapEntry.Tag] = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        foreach (var pair in ScanCompilationForAssetTypes(compilation, cancellationToken))
            tagTypes[pair.Key] = pair.Value;
        return tagTypes
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new TagType(pair.Key, pair.Value))
            .ToImmutableArray();
    }

    private static ConflictInfo ResolveConflict(Compilation compilation, Config config)
        => HasClassNameConflict(compilation, config, out var suggestion)
            ? new ConflictInfo(true, config.Namespace, config.ClassName, suggestion)
            : ConflictInfo.None;

    // ImmutableArray equality is by-reference, so compare element-wise to let equal tables cache.
    private sealed class TagTableComparer : IEqualityComparer<ImmutableArray<TagType>>
    {
        public static readonly TagTableComparer Instance = new();

        public bool Equals(ImmutableArray<TagType> x, ImmutableArray<TagType> y)
        {
            if (x.Length != y.Length)
                return false;
            for (var i = 0; i < x.Length; i++)
            {
                if (!x[i].Equals(y[i]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(ImmutableArray<TagType> obj)
        {
            var hash = 17;
            foreach (var entry in obj)
                hash = unchecked(hash * 31 + entry.GetHashCode());
            return hash;
        }
    }

    private static AssetEntry? ReadAssetEntry(AdditionalText file, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        if (!options.TryGetValue(AssetFolderMetadataKey, out var folder) || string.IsNullOrEmpty(folder))
            return null;

        // Asset-ness is decided by content, not extension: authored assets start with a YAML tag line
        var text = file.GetText(cancellationToken);
        if (text is null || text.Lines.Count == 0)
            return null;
        var firstLine = text.Lines[0].ToString().Trim();
        if (firstLine.Length < 2 || firstLine[0] != '!')
            return null;
        var tag = firstLine.Substring(1).Trim();
        // Package files are not loadable content (an asset folder can contain the sdpkg itself)
        if (tag.Length == 0 || tag == "Package")
            return null;

        var relativePath = GetRelativeAssetPath(folder!, file.Path);
        return relativePath is null ? null : new AssetEntry(relativePath, tag);
    }

    private static IEnumerable<MapEntry> ReadMapEntries(AdditionalText file, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        if (!options.TryGetValue(AssetContentTypeMapMetadataKey, out var isMap) || !string.Equals(isMap, "true", StringComparison.OrdinalIgnoreCase))
            yield break;
        var text = file.GetText(cancellationToken);
        if (text is null)
            yield break;
        foreach (var textLine in text.Lines)
        {
            var line = textLine.ToString().Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;
            var separator = line.IndexOf('|');
            if (separator <= 0 || separator == line.Length - 1)
                continue;
            yield return new MapEntry(line.Substring(0, separator).Trim(), line.Substring(separator + 1).Trim());
        }
    }

    /// <summary>
    /// Asset URL path relative to its asset folder: forward slashes, no extension.
    /// </summary>
    private static string? GetRelativeAssetPath(string folder, string filePath)
    {
        folder = folder.Replace('/', '\\').TrimEnd('\\');
        var file = filePath.Replace('/', '\\');
        if (!file.StartsWith(folder + "\\", StringComparison.OrdinalIgnoreCase))
            return null;
        var relative = file.Substring(folder.Length + 1).Replace('\\', '/');
        var lastSlash = relative.LastIndexOf('/');
        var lastDot = relative.LastIndexOf('.');
        if (lastDot > lastSlash)
            relative = relative.Substring(0, lastDot);
        return relative.Length == 0 ? null : relative;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<AssetEntry> assets, ImmutableArray<TagType> typeTable, Config config, ConflictInfo conflict)
    {
        var entries = assets
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
            .ToList();
        if (entries.Count == 0)
            return;

        if (conflict.HasConflict)
        {
            context.ReportDiagnostic(Diagnostic.Create(ClassNameConflict, Location.None, conflict.Namespace, conflict.ClassName, conflict.Suggestion));
            return;
        }

        // Tag -> emitted runtime type text (resolved from compilation symbols and map files); else untyped
        var tagTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in typeTable)
            tagTypes[entry.Tag] = entry.TypeName;

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
        EmitClass(context, builder, root, config.ClassName, config, tagTypes, indent);
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
    /// Compilation-visible asset types (e.g. a ProjectReference'd plugin): [AssetContentType]
    /// gives the runtime type, the [DataContract] alias is the YAML tag. Only assemblies that
    /// reference Stride.Core.Assets can declare asset types, so anything else is skipped whole.
    /// </summary>
    private static Dictionary<string, string> ScanCompilationForAssetTypes(Compilation compilation, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var assembly in EnumerateAssetTypeAssemblies(compilation))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CollectAssetTypes(assembly.GlobalNamespace, result);
        }
        return result;
    }

    private static IEnumerable<IAssemblySymbol> EnumerateAssetTypeAssemblies(Compilation compilation)
    {
        if (compilation.SourceModule.ReferencedAssemblies.Any(identity => identity.Name == StrideCoreAssetsAssemblyName))
            yield return compilation.Assembly;
        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (reference.Modules.Any(module => module.ReferencedAssemblies.Any(identity => identity.Name == StrideCoreAssetsAssemblyName)))
                yield return reference;
        }
    }

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
                    string? tag = null;
                    INamedTypeSymbol? contentType = null;
                    foreach (var attribute in type.GetAttributes())
                    {
                        var attributeName = attribute.AttributeClass?.ToDisplayString();
                        if (attributeName == AssetContentTypeAttributeFullName)
                            contentType = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol : null;
                        else if (attributeName == DataContractAttributeFullName && attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string alias)
                            tag = alias;
                    }
                    if (contentType is { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public })
                        result[tag ?? type.Name] = contentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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

    private static void EmitClass(SourceProductionContext context, StringBuilder builder, Node node, string className, Config config, Dictionary<string, string> tagTypes, int indent)
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
            var type = tagTypes.TryGetValue(entry.Tag, out var contentType) ? $"{UrlReferenceTypeName}<{contentType}>" : UrlReferenceTypeName;
            builder.Append(pad).Append("    public static readonly ").Append(type).Append(' ').Append(identifier)
                .Append(" = new ").Append(type).Append("(\"").Append(url.Replace("\\", "\\\\").Replace("\"", "\\\"")).AppendLine("\");");
        }
        foreach (var (identifier, child) in children)
            EmitClass(context, builder, child, identifier, config, tagTypes, indent + 1);

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
