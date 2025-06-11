using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Stride.Shaders.Spirv.Generators;


public partial class SPVGenerator
{
    public void CreateSpecification(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {
        var sdsloProvider = grammarProvider
            .Select(static (grammar, _) => grammar.OperandKinds!.Value);

        context.RegisterImplementationSourceOutput(
            sdsloProvider,
            GenerateSDSLSpecification
        );
    }
    public void GenerateSDSLSpecification(SourceProductionContext spc, EquatableArray<OpKind> operandKinds)
    {
        var code = new StringBuilder();
        code
            .AppendLine("namespace Stride.Shaders.Spirv;")
            .AppendLine("")
            .AppendLine("public static class Specification")
            .AppendLine("{");

        foreach (var op in operandKinds)
        {
            if (op.Category == "BitEnum")
            {
                code
                .AppendLine($"[Flags]");
            }
            code
                .AppendLine($"public enum {op.Kind}")
                .AppendLine("{");
            foreach (var enumerant in op.Enumerants!.Value)
                code.AppendLine($"    {enumerant.Name} = {enumerant.Value},");
            code.AppendLine("}");
            code.AppendLine();
        }

        code
            .AppendLine("}");

        spc.AddSource(
            "SDSLSpecification.gen.cs",
            SourceText.From(
                SyntaxFactory
                .ParseCompilationUnit(code.ToString())
                .NormalizeWhitespace()
                .ToFullString(),
                Encoding.UTF8
            )
        );
    }
}