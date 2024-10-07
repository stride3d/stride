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

namespace Stride.Shaders.Spirv.Generators
{
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


            foreach (var instruction in spirvCore.RootElement.GetProperty("instructions").EnumerateArray())
            {
                GenerateInfo(instruction, code);
            }
            foreach (var instruction in spirvSDSL.RootElement.GetProperty("instructions").EnumerateArray())
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
            var kinds = spirvCore.RootElement.GetProperty("operand_kinds").EnumerateArray().Select(x => x.GetProperty("kind").GetString());
            foreach (var kind in kinds)
            {
                code.Append(kind).AppendLine(",");
            }
            code.AppendLine("}");

            context.RegisterPostInitializationOutput(ctx => ctx.AddSource("OperandKind.gen.cs", code.ToSourceText()));

        }

        public void GenerateInfo(JsonElement op, StringBuilder code)
        {
            var opname = op.GetProperty("opname").GetString();
            var spvClass = op.GetProperty("class").GetString();
            if (opname == "OpExtInst")
            {
                code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdResultType, OperandQuantifier.One, \"resultType\", \"GLSL\");");
                code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdResult, OperandQuantifier.One, \"resultId\", \"GLSL\");");
                code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdRef, OperandQuantifier.One, \"set\", \"GLSL\");");
                code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.LiteralInteger, OperandQuantifier.One, \"instruction\", \"GLSL\");");
                code.AppendLine("Instance.Register(SDSLOp.OpExtInst, OperandKind.IdRef, OperandQuantifier.ZeroOrMore, \"values\", \"GLSL\");");
            }
            else if (op.TryGetProperty("operands", out var operands))
            {
                foreach (var operand in operands.EnumerateArray())
                {
                    var hasKind = operand.TryGetProperty("kind", out var kindJson);
                    var hasQuant = operand.TryGetProperty("quantifier", out var quantifierJson);
                    var hasName = operand.TryGetProperty("name", out var nameJson);

                    if (hasKind)
                    {
                        var kind = kindJson.GetString();
                        if (!hasQuant)
                        {
                            code
                                .Append("Instance.Register(SDSLOp.")
                                .Append(opname)
                                .Append(", OperandKind.")
                                .Append(kindJson.GetString())
                                .Append(", OperandQuantifier.One, ")
                                .Append(!hasName ? $"\"{ConvertKindToName(kindJson.GetString())}\"" : $"\"{ConvertOperandName(nameJson.GetString())}\"")
                                .Append($", \"{spvClass}\"")
                                .AppendLine(");");
                        }
                        else
                        {
                            var quant = quantifierJson.GetString();
                            code
                                .Append("Instance.Register(SDSLOp.")
                                .Append(opname)
                                .Append(", OperandKind.")
                                .Append(kindJson.GetString())
                                .Append(", OperandQuantifier.")
                                .Append(ConvertQuantifier(quantifierJson.GetString()))
                                .Append(", ")
                                .Append(!hasName ? $"\"{ConvertNameQuantToName(kind, quant)}\"" : $"\"{ConvertNameQuantToName(nameJson.GetString(), quant)}\"")
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
}
