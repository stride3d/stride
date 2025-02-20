using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG006InvalidAssignMode : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG006";
    private const string Title = "Invalid Assign Mode";
    private const string MessageFormat = "Invalid DataMembermode for the specified [DataMember] member '{0}'. A public/internal/internal protected setter is required for 'DataMemberMode.Assign'.";
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
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataMemberAttribute, dataMemberMode), SymbolKind.Property);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dataMemberMode)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;

        if (!propertySymbol.HasAttribute(dataMemberAttribute))
            return;

        if (!propertySymbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        // 1 is the Enums Value of DataMemberMode for Assign
        if (propertySymbol.HasDataMemberMode(context, dataMemberAttribute, dataMemberMode, 1))
        {
            if (propertySymbol.SetMethod == null || !propertySymbol.SetMethod.IsVisibleToSerializer(hasDataMemberAttribute: true))
            {
                Rule.ReportDiagnostics(context, propertySymbol);
            }
        }
    }
}

