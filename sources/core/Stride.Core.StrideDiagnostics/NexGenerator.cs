using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.StrideDiagnostics;

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
        var syntaxReceiver = (NexSyntaxReceiver)context.SyntaxReceiver;

        foreach (var classDeclaration in syntaxReceiver.TypeDeclarations)
        {
            var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
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
        var baseType = semanticModel.GetDeclaredSymbol(info);
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
