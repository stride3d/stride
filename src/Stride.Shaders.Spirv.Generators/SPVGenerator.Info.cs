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

namespace Stride.Shaders.Spirv.Generators;
public partial class SPVGenerator
{


    public void CreateInfo(IncrementalGeneratorInitializationContext context)
    {

        GenerateKinds(context);

        var code = new StringBuilder();

        code
        .AppendLine("using static Spv.Specification;")
        .AppendLine("")
        .AppendLine("namespace Stride.Shaders.Spirv.Core;")
        .AppendLine("")
        .AppendLine("public partial class InstructionInfo")
        .AppendLine("{")

        .AppendLine("static InstructionInfo()")
        .AppendLine("{")
        ;


        foreach (var instruction in spirvCore!.Instructions)
        {
            GenerateInfo(instruction, code);
        }
        foreach (var instruction in spirvSDSL!.Instructions)
        {
            GenerateInfo(instruction, code);
        }
        code
        .AppendLine("Instance.InitOrder();")

        .AppendLine("}")

        .AppendLine("}");

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("InstructionInfo.gen.cs", code.ToSourceText()));
    }

    private void GenerateKinds(IncrementalGeneratorInitializationContext context)
    {
        var code = new StringBuilder()
        .AppendLine("using static Spv.Specification;")
        .AppendLine("")
        .AppendLine("namespace Stride.Shaders.Spirv.Core;")
        .AppendLine("\n\n")
        .AppendLine("public enum OperandKind")
        .AppendLine("{")

        .AppendLine("None = 0,");
        var kinds = spirvCore!.OperandKinds.Select(x => x.Kind);
        foreach (var kind in kinds)
        {
            code.Append(kind).AppendLine(",");
        }
        code.AppendLine("}");

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("OperandKind.gen.cs", code.ToSourceText()));

    }

    public void GenerateInfo(InstructionData op, StringBuilder code)
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
        else if (op.Operands is List<OperandData> operands)
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

    public static string ConvertNameQuantToName(string name, string quant)
    {
        return (name, quant) switch
        {
            (_, "*") => "values",
            ("event", _) => "eventId",
            ("string", _) => "value",
            ("base", _) => "baseId",
            ("object", _) => "objectId",
            ("default", _) => "defaultId",
            _ => name.Replace("'", "").ToLowerInvariant()
        };
    }

    public static string ConvertQuantifier(string quant)
    {
        if (quant == "*")
            return "ZeroOrMore";
        else if (quant == "?")
            return "ZeroOrOne";
        else return "One";
    }
}

