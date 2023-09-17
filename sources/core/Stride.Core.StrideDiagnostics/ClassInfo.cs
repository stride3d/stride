using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace StrideDiagnostics;
public class ClassInfo
{
    public GeneratorExecutionContext ExecutionContext { get; set; }
    public TypeDeclarationSyntax TypeSyntax { get; set; }
    public ClassDeclarationSyntax SerializerSyntax { get; set; }
    public TypeParameterListSyntax Generics => TypeSyntax.TypeParameterList;
    public bool IsGeneric => TypeSyntax.TypeParameterList != null && TypeSyntax.TypeParameterList.Parameters.Count > 0;
    public INamedTypeSymbol Symbol { get; set; }
    public string TypeName { get; set; }
    public string SerializerName { get; set; }
    public string NamespaceName { get; set; }
    public NexSyntaxReceiver SyntaxReceiver { get; set; }
    public SemanticModel SemanticModel { get => _compilationCache ??= ExecutionContext.Compilation.GetSemanticModel(TypeSyntax.SyntaxTree); }
    private SemanticModel _compilationCache;
    private List<IPropertySymbol> _propertyCache;
    public IEnumerable<IPropertySymbol> AvailableProperties
    {
        get
        {
            if (_propertyCache == null)
            {
                INamedTypeSymbol classSymbol = SemanticModel.GetDeclaredSymbol(TypeSyntax);
                IEnumerable<IPropertySymbol> properties = PropertyAttributeFinder.FilterBasePropertiesRecursive(ref classSymbol);
                return _propertyCache = properties.ToList();
            }
            else
            {
                return _propertyCache;
            }
        }
    }
    public bool IsAbstract()
    {
        return TypeSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.AbstractKeyword));
    }

}
