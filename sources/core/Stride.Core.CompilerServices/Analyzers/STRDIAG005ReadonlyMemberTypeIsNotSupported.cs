using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG005ReadonlyMemberTypeIsNotSupported : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG005";
    private const string Title = "Readonly Member Type is not supported";
    private const string MessageFormat = "The [DataMember] Attribute is applied to a read-only member '{0}' with a non supported type. Only mutable reference types are supported for read-only members.";
    private const string Category = DiagnosticCategory.Serialization;

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, dataMemberAttribute), SymbolKind.Property);
        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, dataMemberAttribute), SymbolKind.Field);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var symbol = (IFieldSymbol)context.Symbol;
        if (!symbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        if (!symbol.HasAttribute(dataMemberAttribute))
            return;
        if (!symbol.IsReadOnly)
            return;
        var fieldType = symbol.Type;

        if (fieldType.IsImmutableType())
        {
            DiagnosticsAnalyzerHelper.ReportDiagnostics(Rule, context, fieldType);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        if (!propertySymbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        if (!propertySymbol.HasAttribute(dataMemberAttribute))
            return;

        if (propertySymbol.GetMethod is null)
            return;

        var setMethod = propertySymbol.SetMethod;
        if (setMethod is null || setMethod.IsVisibleToSerializer(dataMemberAttribute))
        {
            var propertyType = propertySymbol.Type;
            if (propertyType.IsImmutableType())
            {
                DiagnosticsAnalyzerHelper.ReportDiagnostics(Rule, context, propertySymbol);
            }
        }
    }


}
