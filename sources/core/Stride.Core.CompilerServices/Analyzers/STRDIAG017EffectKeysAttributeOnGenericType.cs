using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG017EffectKeysAttributeOnGenericType : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG017";
    private const string Title = "EffectKeysAttribute on Generic Type";
    private const string MessageFormat = "The [EffectKeysAttribute] cannot be applied to generic type '{0}'";
    private const string Category = DiagnosticCategory.Build;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: string.Format(DiagnosticCategory.LinkFormat, DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var effectKeysAttribute = WellKnownReferences.EffectKeysAttribute(context.Compilation);
        if (effectKeysAttribute is null)
            return;

        context.RegisterSymbolAction(
            symbolContext => AnalyzeSymbol(symbolContext, effectKeysAttribute),
            SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol effectKeysAttribute)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (!symbol.HasGenericParameters() || !symbol.HasAttribute(effectKeysAttribute))
            return;

        var location = symbol.Locations.FirstOrDefault(static location => location.IsInSource);
        if (location is not null)
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, symbol.Name));
    }
}
