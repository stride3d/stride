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
            .Select(static (grammar, _) => grammar);

        context.RegisterImplementationSourceOutput(
            sdsloProvider,
            GenerateSDSLSpecification
        );
    }
    public void GenerateSDSLSpecification(SourceProductionContext spc, SpirvGrammar grammar)
    {
        var code = new StringBuilder();
        code
            .AppendLine("namespace Stride.Shaders.Spirv;")
            .AppendLine("")
            .AppendLine("public static partial class Specification")
            .AppendLine("{");

        code.AppendLine($"public static uint MagicNumber {{ get; }} = {grammar.MagicNumber};");
        code.AppendLine($"public static uint MajorVersion {{ get; }} = {grammar.MajorVersion};");
        code.AppendLine($"public static uint MinorVersion {{ get; }} = {grammar.MinorVersion};");
        code.AppendLine($"public static uint Revision {{ get; }} = {grammar.Revision};");
        var operandKinds = grammar.OperandKinds ?? new([]);
        foreach (var op in operandKinds.AsDictionary()!.Values)
        {
            if (op.Category == "Literal" || op.Category == "Id" || op.Category == "Composite")
                continue;
            if (op.Category == "BitEnum")
            {
                code
                .AppendLine($"[Flags]");
            }
            code
                .AppendLine($"public enum {op.Kind}{(op.Category == "BitEnum" ? "Mask" : "")}")
                .AppendLine("{");
            if (op.Enumerants?.AsList() is List<Enumerant> enumerants)
            {
                foreach (var enumerant in enumerants)
                    code.AppendLine($"    {(char.IsDigit(enumerant.Name[0]) ? op.Kind : "")}{enumerant.Name} = {enumerant.Value},");
            }
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