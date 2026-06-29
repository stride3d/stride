using System.Collections.Generic;
using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

// Warns when an IProjectAsset type's [AssetDescription] extension is missing from
// StrideProjectAssetExtensions (the build property selecting which project-source files reach the
// .sdbuild manifest); forgetting it silently drops those assets. Skips .cs and generator assets,
// which aren't asset-build inputs.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG011UndeclaredProjectAssetExtension : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG011";
    private const string Title = "Project-asset extension not declared in StrideProjectAssetExtensions";
    private const string MessageFormat = "Asset type '{0}' declares file extension '{1}', which is missing from StrideProjectAssetExtensions. Append it in the project's build .targets so the asset compiler captures it into the .sdbuild manifest.";
    private const string Category = DiagnosticCategory.Build;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: string.Format(DiagnosticCategory.LinkFormat, DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var projectAsset = WellKnownReferences.IProjectAsset(context.Compilation);
        var assetDescription = WellKnownReferences.AssetDescriptionAttribute(context.Compilation);

        // Not an asset-defining compilation (doesn't reference Stride.Core.Assets).
        if (projectAsset is null || assetDescription is null)
            return;

        // Without the build property visible we can't validate — stay silent rather than guess.
        // editorconfig can't carry ';' (comment char), so the build passes a comma-separated copy.
        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                "build_property.StrideProjectAssetExtensionsForAnalyzer", out var raw))
            return;

        var declared = ParseExtensions(raw);
        var generatorAsset = WellKnownReferences.IProjectFileGeneratorAsset(context.Compilation);

        context.RegisterSymbolAction(
            symbolContext => AnalyzeSymbol(symbolContext, projectAsset, generatorAsset, assetDescription, declared),
            SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol projectAsset,
        INamedTypeSymbol? generatorAsset, INamedTypeSymbol assetDescription, HashSet<string> declared)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract)
            return;

        if (!symbol.AllInterfaces.Contains(projectAsset, SymbolEqualityComparer.Default))
            return;

        // Generator assets (e.g. visual scripts) emit code at design time; their source is not an asset-build input.
        if (generatorAsset is not null && symbol.AllInterfaces.Contains(generatorAsset, SymbolEqualityComparer.Default))
            return;

        if (!symbol.TryGetAttribute(assetDescription, out var attribute))
            return;
        if (attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not string fileExtensions)
            return;

        foreach (var extension in ParseExtensions(fileExtensions))
        {
            // .cs is C# compiled into the assembly, never built from project source by the asset compiler.
            if (extension == ".cs")
                continue;
            if (declared.Contains(extension))
                continue;

            foreach (var location in symbol.Locations)
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, symbol.Name, extension));
        }
    }

    // Split on ',' / ';', trim, lowercase, ensure leading '.'.
    private static HashSet<string> ParseExtensions(string value)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in value.Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var extension = part.Trim().ToLowerInvariant();
            if (extension.Length == 0)
                continue;
            if (!extension.StartsWith("."))
                extension = "." + extension;
            set.Add(extension);
        }
        return set;
    }
}
