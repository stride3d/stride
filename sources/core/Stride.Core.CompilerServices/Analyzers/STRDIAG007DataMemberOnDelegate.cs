using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG007DataMemberOnDelegate : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG007";
    private const string Title = "Invalid [DataMember] Attribute";
    private const string MessageFormat = "Invalid [DataMember] Attribute on the member '{0}'. A Delegate is not serializable.";
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
        if (dataMemberAttribute is null)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, dataMemberAttribute), SymbolKind.Property);
        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, dataMemberAttribute), SymbolKind.Field);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var fieldSymbol = (IFieldSymbol)context.Symbol;
        if (!fieldSymbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        if (!fieldSymbol.HasAttribute(dataMemberAttribute))
            return;

        var fieldType = fieldSymbol.Type;

        if (fieldType.TypeKind == TypeKind.Delegate)
        {
            Rule.ReportDiagnostics(context, fieldSymbol);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        if (!propertySymbol.IsVisibleToSerializer(dataMemberAttribute))
            return;

        if (!propertySymbol.HasAttribute(dataMemberAttribute))
            return;
        var propertyType = propertySymbol.Type;

        if (propertyType.TypeKind == TypeKind.Delegate)
        {
            Rule.ReportDiagnostics(context, propertySymbol);
        }
    }
}
