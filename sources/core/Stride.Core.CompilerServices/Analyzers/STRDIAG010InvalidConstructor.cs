using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG010InvalidConstructor : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG010";
    private const string Title = "Invalid constructor";
    private const string MessageFormat = "The type '{0}' doesn't have a public parameterless constructor, which is needed for serialization.";
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
        var dataContractAttribute = WellKnownReferences.DataContractAttribute(context.Compilation);

        if (dataContractAttribute is null)
            return;

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataContractAttribute), SymbolKind.NamedType);

    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dataContractAttribute)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.IsAbstract)
            return;

        if (symbol.HasAttribute(dataContractAttribute))
        {
            TryReportDiagnostics(symbol, context);
            return;
        }

        var type = symbol.BaseType;
        bool isInherited = false;
        while (type != null)
        {
            // Check if the type has the specified DataContractAttribute through inheritance
            if (type.TryGetAttribute(dataContractAttribute,out var datacontractData) && datacontractData.AttributeConstructor is not null)
            {
                if (datacontractData is { NamedArguments: [.., { Key: "Inherited" , Value: var inherited } ] })
                {
                    isInherited = (bool)inherited.Value!;
                } 
                break;
            }
            type = type.BaseType;
        }
        if(isInherited)
        {
            TryReportDiagnostics(symbol, context);
        }
    }
    private static void TryReportDiagnostics(INamedTypeSymbol symbol,SymbolAnalysisContext context)
    {
        if (HasPublicEmptyConstructor(symbol) || IsCommandRunInProcess(symbol))
        {
            return;
        }

        Rule.ReportDiagnostics(context, symbol);
    }

    /// <summary>
    /// Whether the type is a build command (<c>Stride.Core.BuildEngine.Command</c>) that runs in the process that made it.
    /// Commands are data contracts, but only one that runs in a process of its own (<c>ShouldSpawnNewProcess</c>) is serialized.
    /// </summary>
    private static bool IsCommandRunInProcess(INamedTypeSymbol symbol)
    {
        for (var type = symbol; type != null; type = type.BaseType)
        {
            if (type.Name == "Command" && type.ContainingNamespace?.ToDisplayString() == "Stride.Core.BuildEngine")
                return true;
            if (type.GetMembers("ShouldSpawnNewProcess").OfType<IMethodSymbol>().Any(x => x.IsOverride && x.Parameters.Length == 0))
                return false;
        }
        return false;
    }

    private static bool HasPublicEmptyConstructor(INamedTypeSymbol type) 
        => type.Constructors.Any(x => x.Parameters.Length == 0 && x.DeclaredAccessibility == Accessibility.Public);
}
