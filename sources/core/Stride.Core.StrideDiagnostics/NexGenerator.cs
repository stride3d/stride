using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace StrideDiagnostics;

[Generator]
public class NexGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Debugger.Launch();
        context.RegisterForSyntaxNotifications(() => new NexSyntaxReceiver());
    }
    private Diagnoser classGenerator { get; set; } = new();
    public void Execute(GeneratorExecutionContext context)
    {
        NexSyntaxReceiver syntaxReceiver = (NexSyntaxReceiver)context.SyntaxReceiver;

        foreach (TypeDeclarationSyntax classDeclaration in syntaxReceiver.TypeDeclarations)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (!HasDataContractAttribute(classDeclaration, semanticModel))
            {
                continue;
            }
            ClassInfo info = new()
            {
                ExecutionContext = context,
                TypeSyntax = classDeclaration,
                SyntaxReceiver = syntaxReceiver,
                Symbol = semanticModel.GetDeclaredSymbol(classDeclaration),

            };
            classGenerator.StartCreation(info);
        }
    }
    /// <summary>
    /// Checks if the <see cref="TypeDeclarationSyntax"/> has a [DataContract] attribute or [Stride.Core.DataContract]
    /// As there is no validation possible to decide if its from System or Stride this must be validated later again.
    /// </summary>
    /// <param name="info">The Type to check</param>
    /// <returns>True if it has a DataContract Attribute, but its not sure if its the one from Stride</returns>
    private bool HasDataContractAttribute(TypeDeclarationSyntax info, SemanticModel semanticModel)
    {
        // Check if the current class has [DataContract] attribute
        if (info.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => attribute.Name.ToString() == "DataContract"))
        {
            return true;
        }

        // Check if any base class has [DataContract(Inherited = true)] attribute
        INamedTypeSymbol baseType = semanticModel.GetDeclaredSymbol(info);
        while (baseType != null)
        {

            if (
                baseType.GetAttributes().Any(attr =>
                    attr.AttributeClass.Name == "DataContractAttribute"
                     && attr.AttributeClass.ContainingNamespace.ContainingModule.Name == "Stride.Core.dll" &&
                    attr.NamedArguments.Any(arg =>
                        arg.Key == "Inherited" && (bool)arg.Value.Value == true)))
            {

                return true;
            }
            // Check the next base class
            baseType = baseType.BaseType;
        }

        return false;
    }
    public const string CompilerServicesDiagnosticCategory = "Stride.CompilerServices";
}
