using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG002InvalidContentMode : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG002";
    private const string Title = "Invalid Content Mode";
    private const string MessageFormat = "The 'DataMemberMode.Content' is not valid for the member '{0}'. Only mutable reference types are supported for 'DataMemberMode.Content' Mode members.";
    private const string Category = DiagnosticCategory.Serialization;

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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        var dataMemberMode = WellKnownReferences.DataMemberMode(context.Compilation);

        if (dataMemberAttribute is null || dataMemberMode is null)
            return;

        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, dataMemberAttribute, dataMemberMode), SymbolKind.Field);
        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, dataMemberAttribute, dataMemberMode), SymbolKind.Property);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dataMembermode)
    {
        var symbol = (IFieldSymbol)context.Symbol;
        if (!symbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        if (!symbol.HasAttribute(dataMemberAttribute))
            return;

        // 2 is the Enums Value of DataMemberMode for Content
        if (!symbol.HasDataMemberMode(context, dataMemberAttribute, dataMembermode, 2))
            return;
        var fieldType = symbol.Type;
        if (fieldType.IsImmutableType())
        {
            Rule.ReportDiagnostics(context, fieldType);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dataMembermode)
    {
        var symbol = (IPropertySymbol)context.Symbol;
        if (!symbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        if (!symbol.HasAttribute(dataMemberAttribute))
            return;

        // 2 is the Enums Value of DataMemberMode for Content
        if (!symbol.HasDataMemberMode(context, dataMemberAttribute, dataMembermode, 2))
            return;
        var fieldType = symbol.Type;
        if (fieldType.IsImmutableType())
        {
            Rule.ReportDiagnostics(context, fieldType);
        }
    }
}
