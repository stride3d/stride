using System.Collections.Generic;
using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

// Warns when an asset type's extension and content type have no matching [assembly: AssetFileExtension] in the references.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG014UndeclaredAssetFileExtension : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG014";
    private const string Title = "Asset file extension not declared by a runtime assembly";
    private const string MessageFormat = "Asset type '{0}' compiles '{1}' files to '{2}', which no referenced assembly declares. Add [assembly: Stride.Core.Serialization.AssetFileExtension(\"{1}\", typeof({2}))] to the runtime assembly games reference ({3}), so their asset URL constants are typed.";
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
        var assetDescription = WellKnownReferences.AssetDescriptionAttribute(context.Compilation);
        var assetContentType = WellKnownReferences.AssetContentTypeAttribute(context.Compilation);
        var assetFileExtension = WellKnownReferences.AssetFileExtensionAttribute(context.Compilation);

        // Not an asset-defining compilation (doesn't reference Stride.Core.Assets), or no declaration attribute to point to
        if (assetDescription is null || assetContentType is null || assetFileExtension is null)
            return;

        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in new[] { context.Compilation.Assembly }.Concat(context.Compilation.SourceModule.ReferencedAssemblySymbols))
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, assetFileExtension)
                    && attribute.ConstructorArguments.Length == 2
                    && attribute.ConstructorArguments[0].Value is string extension
                    && attribute.ConstructorArguments[1].Value is INamedTypeSymbol type)
                    declared.Add(Key(extension, type));
            }
        }

        context.RegisterSymbolAction(
            symbolContext => AnalyzeSymbol(symbolContext, assetDescription, assetContentType, declared),
            SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol assetDescription, INamedTypeSymbol assetContentType, HashSet<string> declared)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract)
            return;

        // Both attributes are inherited
        if (FindAttribute(symbol, assetDescription) is not { } description || FindAttribute(symbol, assetContentType) is not { } content)
            return;
        if (description.ConstructorArguments.Length == 0 || description.ConstructorArguments[0].Value is not string fileExtensions)
            return;
        if (content.ConstructorArguments.Length == 0 || content.ConstructorArguments[0].Value is not INamedTypeSymbol contentType)
            return;

        foreach (var extension in fileExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = extension.Trim();
            if (trimmed.Length == 0 || declared.Contains(Key(trimmed, contentType)))
                continue;

            foreach (var location in symbol.Locations)
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, symbol.Name, trimmed, contentType.ToDisplayString(), contentType.ContainingAssembly?.Name));
        }
    }

    private static AttributeData? FindAttribute(INamedTypeSymbol symbol, INamedTypeSymbol attribute)
    {
        for (var type = symbol; type is not null; type = type.BaseType)
        {
            if (type.TryGetAttribute(attribute, out var data))
                return data;
        }
        return null;
    }

    private static string Key(string extension, ITypeSymbol type) => extension.Trim() + "|" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
