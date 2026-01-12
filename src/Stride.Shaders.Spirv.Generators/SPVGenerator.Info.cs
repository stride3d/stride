using Microsoft.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{

    public void CreateParameterizedFuncs(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {

        context.RegisterImplementationSourceOutput(
            grammarProvider,
            GenerateParameterizedFunctions
        );
    }
    public void CreateInfo(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {

        GenerateKinds(context, grammarProvider);
        context.RegisterImplementationSourceOutput(
            grammarProvider,
            GenerateInstructionInformation
        );
    }

    public void GenerateParameterizedFunctions(SourceProductionContext context, SpirvGrammar grammar)
    {
        if (grammar.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> dict)
        {
            var code = new StringBuilder();
            code.AppendLine(@"
            using static Stride.Shaders.Spirv.Specification;
            namespace Stride.Shaders.Spirv.Core;
            
            public static class ParameterizedFlags
            {"
            );
            var selection =
                dict.Values
                .Where(enumeration => enumeration.Enumerants?.AsList() is List<Enumerant> { Count: > 0 } l && l.Any(e => e.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 }))
                .SelectMany(enumeration => (enumeration.Enumerants?.AsList() ?? []).Select(e => (enumeration.Kind, e)))
                .Where(x => x.e.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 });
            foreach (var (kind, enumerant) in selection)
            {
                var realKind = kind;
                if (dict[kind].Category is "BitEnum")
                    realKind = $"{kind}Mask";
                code.Append($"public static ParameterizedFlag<{realKind}> {kind}{enumerant.Name}(");
                foreach (var param in enumerant.Parameters?.AsList() ?? [])
                {
                    code.Append($"{param.CSType} {param.Name}");
                    if (param != enumerant.Parameters?.AsList()?.Last())
                        code.Append(", ");
                }
                code.AppendLine(")")
                .Append($"    => new ParameterizedFlag<{realKind}>({realKind}.{enumerant.Name}, [{string.Join(", ", (enumerant.Parameters?.AsList() ?? []).Select(x => x.CSType switch
                {
                    "float" => $"BitConverter.SingleToInt32({x.Name})",
                    "string" => $".. {x.Name}.AsDisposableLiteralValue().Words",
                    "int" => x.Name,
                    _ => $"(int){x.Name}",

                }))}]);");
            }
            code.AppendLine("}");
            context.AddSource(
                "ParameterizedFlags.gen.cs",
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
    static void GenerateInstructionInformation(SourceProductionContext spc, SpirvGrammar grammar)
    {
        var code = new StringBuilder();
        code
            .AppendLine(@"
            using static Stride.Shaders.Spirv.Specification;
            namespace Stride.Shaders.Spirv.Core;
            
            public partial class InstructionInfo
            {
            static InstructionInfo()
            {"
        );
        if (grammar.Instructions?.AsList() is List<InstructionData> instructions)
            foreach (var instruction in instructions)
                if (!instruction.OpName.Contains("GLSL"))
                    GenerateInfo(instruction, code, grammar);

        code.AppendLine("Instance.InitOrder();}}");
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
                if (kinds.AsDictionary() is Dictionary<string, OpKind> dict)
                {
                    builder
                        .AppendLine("using static Stride.Shaders.Spirv.Specification;")
                        .AppendLine("")
                        .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                        .AppendLine("")
                        .AppendLine("public enum OperandKind")
                        .AppendLine("{")
                        .AppendLine("    None,");
                    foreach (var kind in dict.Values)
                        builder.AppendLine($"    {kind.Kind},");
                    builder
                        .AppendLine("}");

                    builder.AppendLine()
                    .AppendLine("public static class OperandKindExtensions")
                    .AppendLine("{")
                    .AppendLine("public static bool IsEnum(this OperandKind kind)")
                    .AppendLine("{")
                    .AppendLine("return kind switch")
                    .AppendLine("{");
                    foreach (var kind in dict.Values.Where(k => k.Category.EndsWith("Enum")))
                        builder.AppendLine($"    OperandKind.{kind.Kind} => true,");
                    builder.AppendLine("    _ => false")
                    .AppendLine("};")
                    .AppendLine("}")
                    .AppendLine("public static string? ConvertEnumValueToString(this OperandKind kind, int value)")
                    .AppendLine("{")
                    .AppendLine("return kind switch")
                    .AppendLine("{");
                    foreach (var kind in dict.Values.Where(k => k.Category.EndsWith("Enum")))
                        builder.AppendLine($"    OperandKind.{kind.Kind} => (({kind.Kind}{(kind.Category is "BitEnum" ? "Mask" : "")})value).ToString(),");
                    builder.AppendLine("    _ => null")
                    .AppendLine("};")
                    .AppendLine("}")
                    .AppendLine("}");
                }
                spc.AddSource("OperandKind.gen.cs", SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit(builder.ToString())
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                ));
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

    public static void GenerateInfo(InstructionData op, StringBuilder code, SpirvGrammar grammar)
    {

        var opname = op.OpName;
        var spvClass = op.Class;
        if (op.Operands?.AsList() is List<OperandData> operands && grammar.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> dict)
        {
            foreach (var operand in operands)
            {

                code.Append($"Instance.Register(Op.{opname}, OperandKind.{operand.Kind ?? "<error>"}, OperandQuantifier.{operand.Quantifier switch { "*" => "ZeroOrMore", "?" => "ZeroOrOne", _ => "One" }}, \"{operand.Name}\", \"{spvClass ?? "Debug"}\"");
                if (operand.IsParameterized && dict.TryGetValue(operand.Kind ?? throw new Exception("Operand is null in registering"), out var opkind) && opkind.Enumerants?.AsList() is List<Enumerant> enumerants && enumerants.Any(x => x.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 }))
                {
                    // code.Append($", [{string.Join(", ", opkind.Enumerants?.Select(x => $"new({x.Name ?? "null"}, OperandKind.{x.})") ?? [])}]");
                    code.Append(", new() {")
                    .Append(
                        string.Join(
                            ", ",
                            enumerants
                            .Where(e => e.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 })
                            .Select(
                                enumerant =>
                                    $"[new(OperandKind.{operand.Kind}, {enumerant.Value})] = [{string.Join(", ", enumerant.Parameters?.AsList().Select(param => $"new(\"{param.Name ?? ConvertKindToName(param.Kind)}\", OperandKind.{param.Kind})") ?? [])}]"
                            )
                        )
                    )
                    .Append("});");
                }
                else
                    code.AppendLine(", []);");
            }
        }
        else
            code.Append("Instance.Register(Op.").Append(opname).AppendLine(", OperandKind.None, null, \"Debug\");");
    }
}