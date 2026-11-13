using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG016MissingEffectKeysAttribute : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG016";
    private const string Title = "Missing EffectKeysAttribute";
    private const string MessageFormat = "Type '{0}' declares a static field whose type derives from ParameterKey, but is not marked with [EffectKeysAttribute]";
    private const string Category = DiagnosticCategory.Build;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
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
        var parameterKey = WellKnownReferences.ParameterKey(context.Compilation);
        var generatedCodeAttribute = WellKnownReferences.GeneratedCodeAttribute(context.Compilation);
        if (effectKeysAttribute is null || parameterKey is null || generatedCodeAttribute is null)
            return;

        context.RegisterSymbolAction(
            symbolContext => AnalyzeSymbol(symbolContext, effectKeysAttribute, parameterKey, generatedCodeAttribute),
            SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(
        SymbolAnalysisContext context,
        INamedTypeSymbol effectKeysAttribute,
        INamedTypeSymbol parameterKey,
        INamedTypeSymbol generatedCodeAttribute)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct) || symbol.HasAttribute(effectKeysAttribute))
            return;

        if (symbol.HasGenericParameters())
            return; // Applying the attribute to a generic type is not supported, so we skip it.

        foreach (var member in symbol.GetMembers())
        {
            if (member is not IFieldSymbol { IsStatic: true, IsImplicitlyDeclared: false } field
                || field.HasAttribute(generatedCodeAttribute)
                || !IsAssignableToParameterKey(field.Type, parameterKey))
                continue;

            var location = symbol.Locations.FirstOrDefault(static location => location.IsInSource);
            if (location is not null)
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, symbol.Name));
            return;
        }
    }

    private static bool IsAssignableToParameterKey(ITypeSymbol type, INamedTypeSymbol parameterKey)
    {
        for (var current = type; current is not null; current = current.BaseType?.OriginalDefinition)
        {
            if (SymbolEqualityComparer.Default.Equals(current, parameterKey))
                return true;
        }

        return false;
    }
}
