using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Security.Claims;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{


    public void CreateInfo(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {

        GenerateKinds(context, grammarProvider);

        IncrementalValueProvider<EquatableArray<InstructionData>> infoProvider =
            grammarProvider
            .SelectMany(static (grammar, _) => grammar.Instructions!.Value)
            .Where(static x => x.OpName is not null && !x.OpName.Contains("GLSL"))
            .Collect()
            .Select(static (arr, _) => new EquatableArray<InstructionData>([.. arr]));

        context.RegisterImplementationSourceOutput(
            infoProvider,
            GenerateInstructionInformation
        );
    }
    static void GenerateInstructionInformation(SourceProductionContext spc, EquatableArray<InstructionData> instructions)
    {
        var code = new StringBuilder();
        code
            .AppendLine("using static Stride.Shaders.Spirv.Specification;")
            .AppendLine("")
            .AppendLine("namespace Stride.Shaders.Spirv.Core;")
            .AppendLine("")
            .AppendLine("public partial class InstructionInfo")
            .AppendLine("{")
            .AppendLine("static InstructionInfo()")
            .AppendLine("{");
        foreach (var instruction in instructions)
            GenerateInfo(instruction, code);

        code
            .AppendLine("Instance.InitOrder();")
            .AppendLine("}")
            .AppendLine("}");
        spc.AddSource(
            "InstructionInfo.gen.cs",
            SourceText.From(
                SyntaxFactory
                .ParseCompilationUnit(code.ToString())
                .NormalizeWhitespace()
                .ToFullString(),
                Encoding.UTF8
            )
        );
    }

    private void GenerateKinds(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {
        var kindsProvider = grammarProvider
            .Select(static (grammar, _) => grammar.OperandKinds!.Value);

        context.RegisterImplementationSourceOutput(kindsProvider,
            static (spc, kinds) =>
            {
                var builder = new StringBuilder();
                builder
                    .AppendLine("using static Stride.Shaders.Spirv.Specification;")
                    .AppendLine("")
                    .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                    .AppendLine("")
                    .AppendLine("public enum OperandKind")
                    .AppendLine("{")
                    .AppendLine("    None,");
                if(kinds.AsDictionary() is Dictionary<string, OpKind> dict)
                foreach (var kind in dict.Values)
                    builder.AppendLine($"    {kind.Kind},");
                builder
                    .AppendLine("}");
                spc.AddSource("OperandKind.gen.cs", builder.ToString());
            }
        );
        // var code = new StringBuilder()
        // .AppendLine("using static Stride.Shaders.Spirv.Specification;")
        // .AppendLine("")
        // .AppendLine("namespace Stride.Shaders.Spirv.Core;")
        // .AppendLine("\n\n")
        // .AppendLine("public enum OperandKind")
        // .AppendLine("{")

        // .AppendLine("None = 0,");
        // var kinds = spirvCore!.OperandKinds.Select(x => x.Kind);
        // foreach (var kind in kinds)
        // {
        //     code.Append(kind).AppendLine(",");
        // }
        // code.AppendLine("}");

        // context.RegisterPostInitializationOutput(ctx => ctx.AddSource("OperandKind.gen.cs", code.ToSourceText()));

    }

    public static void GenerateInfo(InstructionData op, StringBuilder code)
    {

        var opname = op.OpName;
        var spvClass = op.Class;
        if (opname == "OpExtInst")
        {
            code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdResultType, OperandQuantifier.One, \"resultType\", \"GLSL\");");
            code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdResult, OperandQuantifier.One, \"resultId\", \"GLSL\");");
            code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdRef, OperandQuantifier.One, \"set\", \"GLSL\");");
            code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.LiteralInteger, OperandQuantifier.One, \"instruction\", \"GLSL\");");
            code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdRef, OperandQuantifier.ZeroOrMore, \"values\", \"GLSL\");");
        }
        else if (op.Operands is EquatableList<OperandData> operands)
        {
            foreach (var operand in operands)
            {
                // var hasKind = operand.Kind;
                // var hasQuant = operand.Quantifier;
                // var hasName = operand.Name;

                if (operand.Kind is string kind)
                {
                    if (operand.Quantifier is string quant)
                    {
                        code
                            .Append("Instance.Register(SDSLOp.")
                            .Append(opname)
                            .Append(", OperandKind.")
                            .Append(kind)
                            .Append(", OperandQuantifier.")
                            .Append(ConvertQuantifier(quant))
                            .Append(", ")
                            .Append(operand.Name is null ? $"\"{ConvertNameQuantToName(kind, quant)}\"" : $"\"{ConvertNameQuantToName(operand.Name, quant)}\"")
                            .Append($", \"{spvClass}\"")
                            .AppendLine(");");
                    }
                    else
                    {
                        code
                            .Append("Instance.Register(SDSLOp.")
                            .Append(opname)
                            .Append(", OperandKind.")
                            .Append(kind)
                            .Append(", OperandQuantifier.One, ")
                            .Append(operand.Name is null ? $"\"{ConvertKindToName(kind)}\"" : $"\"{ConvertOperandName(operand.Name)}\"")
                            .Append($", \"{spvClass}\"")
                            .AppendLine(");");
                    }

                }
            }
        }
        else
        {
            code.Append("Instance.Register(SDSLOp.").Append(opname).AppendLine(", OperandKind.None, null, \"Debug\");");
        }
    }


}

