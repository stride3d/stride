using AngleSharp.Dom;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using AngleSharp;

namespace Stride.Shaders.Spirv.Generators;


[Generator]
public partial class SPVGenerator : IIncrementalGenerator
{
    SpirvGrammar? spirvCore;
    SpirvGrammar? spirvGlsl;
    SpirvGrammar? spirvSDSL;

    IDocument? unifiedDoc;
    IDocument? glslDoc;

    Dictionary<string, OpKind> operandKinds = [];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // #if DEBUG
        //             if (!Debugger.IsAttached)
        //                 Debugger.Launch();
        // #endif
        var assembly = typeof(SPVGenerator).GetTypeInfo().Assembly;
        string resourceCoreName =
            assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("spirv.core.grammar.json"));

        string resourceGlslName =
            assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("extinst.glsl.std.450.grammar.json"));

        string resourceSDSLName =
            assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("spirv.sdsl.grammar-ext.json"));

        string resourceUnifiedName =
            assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("SPIRV.html"));
        string resourceGlslRegistryName =
            assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("GLSL.std.450.html"));




        spirvCore = JsonSerializer.Deserialize<SpirvGrammar>(new StreamReader(assembly.GetManifestResourceStream(resourceCoreName)).ReadToEnd());
        spirvGlsl = JsonSerializer.Deserialize<SpirvGrammar>(new StreamReader(assembly.GetManifestResourceStream(resourceGlslName)).ReadToEnd());
        spirvSDSL = JsonSerializer.Deserialize<SpirvGrammar>(new StreamReader(assembly.GetManifestResourceStream(resourceSDSLName)).ReadToEnd());

        var config = Configuration.Default.WithDefaultLoader();
        var htmlContext = BrowsingContext.New(config);
        var documentTask = htmlContext.OpenAsync(req => req.Content(new StreamReader(assembly.GetManifestResourceStream(resourceUnifiedName)).ReadToEnd()));
        documentTask.Wait();
        unifiedDoc = documentTask.Result;
        var glslDocumentTask = htmlContext.OpenAsync(req => req.Content(new StreamReader(assembly.GetManifestResourceStream(resourceGlslRegistryName)).ReadToEnd()));
        glslDocumentTask.Wait();
        glslDoc = glslDocumentTask.Result;

        foreach (var o in spirvCore!.OperandKinds)
            operandKinds[o.Kind] = o;

        CreateInfo(context);
        CreateSDSLOp(context);

        var code = new StringBuilder();

        code
        .AppendLine("using static Spv.Specification;")
        .AppendLine("namespace Stride.Shaders.Spirv.Core.Buffers;")
        .AppendLine("")
        .AppendLine("public static class SpirvBufferExtensions")
        .AppendLine("{");

        var instructions = spirvCore!.Instructions;
        var sdslInstructions = spirvSDSL!.Instructions;
        var glslInstruction = spirvGlsl!.Instructions;

        instructions.ForEach(x => CreateOperation(x, code));
        sdslInstructions.ForEach(x => CreateOperation(x, code));
        glslInstruction.ForEach(x => CreateGlslOperation(x, code));

        code.AppendLine("}");

        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                "SpirvBufferExtensions.gen.cs",
                code.ToSourceText());
        });
    }

    public static string AddDocComment(IHtmlCollection<IElement>? cells)
    {
        var code = new StringBuilder();
        if (cells.FirstOrDefault() is IElement element)
        {
            var split = element.TextContent.Split('\n');
            code.AppendLine("/// <summary>").Append("/// <para><c>").Append(split[0]).AppendLine("</c></para>");
            foreach (var t in split.Skip(1))
                if(!string.IsNullOrEmpty(t))
                    code.Append("/// <para>")
                        .Append(t.Replace("<id>", "<c>id</c>"))
                        .AppendLine("</para>");
            code.AppendLine("/// </summary>");
        }
        return code.ToString();
    }

    public void CreateOperation(InstructionData op, StringBuilder code)
    {
        var opname = op.OpName;
        var cells = unifiedDoc!.QuerySelectorAll($"p.tableblock:has(#{opname})");
        var comment = AddDocComment(cells);
        code.AppendLine(comment);
        if (opname == "OpConstant")
        {
            code
                .AppendLine("public static Instruction AddOpConstant<TValue>(this SpirvBuffer buffer, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")
                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")

                .AppendLine("}");


            code.AppendLine(comment);
            code
                .AppendLine("public static Instruction InsertOpConstant<TValue>(this SpirvBuffer buffer, int position, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")
                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")

                .AppendLine("}");

        }
        else if (opname == "OpSpecConstant")
        {
            code
                .AppendLine("public static Instruction AddOpSpecConstant<TValue>(this SpirvBuffer buffer, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpSpecConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")
                .AppendLine("}");
            code.AppendLine(comment);
            code
                .AppendLine("public static Instruction InsertOpSpecConstant<TValue>(this SpirvBuffer buffer, int position, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpSpecConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")
                .AppendLine("}");
        }
        else if (opname!.StartsWith("OpDecorate"))
        {
            code
                .Append("public static Instruction Add").Append(opname).Append("(this SpirvBuffer buffer, IdRef target, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(target) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpDecorate, target, ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
            code.AppendLine(comment);
            code
                .Append("public static Instruction Insert").Append(opname).Append("(this SpirvBuffer buffer, int position, IdRef target, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(target) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpDecorate, target, ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
        }
        else if (opname.StartsWith("OpMemberDecorate"))
        {
            code
                .Append("public static Instruction Add").Append(opname).Append("(this SpirvBuffer buffer, IdRef structureType, LiteralInteger member, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(structureType) + buffer.GetWordLength(member) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpMemberDecorate, ..structureType.AsSpirvSpan(), ..member.AsSpirvSpan(), ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
            code.AppendLine(comment);
            code
                .Append("public static Instruction Insert").Append(opname).Append("(this SpirvBuffer buffer, int position, IdRef structureType, LiteralInteger member, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(structureType) + buffer.GetWordLength(member) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpMemberDecorate, ..structureType.AsSpirvSpan(), ..member.AsSpirvSpan(), ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
        }

        else if (op.Operands is not null && op.Operands.Count > 0)
        {
            var parameters = ConvertOperandsToParameters(op);
            var parameterNames = ConvertOperandsToParameterNames(op);
            var hasResultId = parameterNames.Contains("resultId") && opname != "OpExtInst";
            if (hasResultId)
            {
                parameters.Remove(parameters.First(x => x.Contains("resultId")));
            }
            var paramsParameters = parameters.Where(x => x.StartsWith("Span"));
            var nullableParameters = parameters.Where(x => x.Contains("?"));
            var normalParameters = parameters.Where(x => !x.Contains("?") && !x.StartsWith("Span"));

            code
                .Append("public static Instruction Add")
                .Append(opname)
                .Append("(this SpirvBuffer buffer")
                .Append(hasResultId ? ", IdResult resultId" : "")
                .Append(normalParameters.Count() + nullableParameters.Count() + paramsParameters.Count() == 0 ? "" : ", ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                ;
            code.Append("var wordLength = 1").Append(parameterNames.Any() ? " + " : "").Append(string.Join(" + ", parameterNames.Select(x => $"buffer.GetWordLength({x})"))).AppendLine(";");
            code
                .AppendLine($"return buffer.Add([wordLength << 16 | (int)SDSLOp.{opname}, {string.Join(", ", parameterNames.Select(x => $"..{x}.AsSpirvSpan()"))}]);")
                .AppendLine("}");



            code.AppendLine(comment);
            code
                .Append("public static Instruction Insert")
                .Append(opname)
                .Append("(this SpirvBuffer buffer, int position")
                .Append(hasResultId ? ", IdResult resultId" : "")
                .Append(normalParameters.Count() + nullableParameters.Count() + paramsParameters.Count() == 0 ? "" : ", ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                ;
            code.Append("var wordLength = 1").Append(parameterNames.Any() ? " + " : "").Append(string.Join(" + ", parameterNames.Select(x => $"buffer.GetWordLength({x})"))).AppendLine(";");
            code
                .AppendLine($"return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.{opname}, {string.Join(", ", parameterNames.Select(x => $"..{x}.AsSpirvSpan()"))}]);")
                .AppendLine("}");
        }
        else
        {
            code
                .Append("public static Instruction Add")
                .Append(opname)
                .AppendLine("(this SpirvBuffer buffer)")
                .AppendLine("{")

                    .AppendLine($"return buffer.Add([1 << 16 | (int)SDSLOp.{opname}]);")

                .AppendLine("}");
            code.AppendLine(comment);
            code
                .Append("public static Instruction Insert")
                .Append(opname)
                .AppendLine("(this SpirvBuffer buffer, int position)")
                .AppendLine("{")
                    .AppendLine($"return buffer.Insert(position, [1 << 16 | (int)SDSLOp.{opname}]);")
                .AppendLine("}");
        }
    }

    public void CreateGlslOperation(InstructionData op, StringBuilder code)
    {
        var opname = op.OpName;
        var opcode = op.OpCode;
        
        if (op.Operands is not null)
        {
            var parameters = ConvertOperandsToParameters(op);
            parameters.Add("int set");

            var parameterNames = ConvertOperandsToParameterNames(op);
            parameterNames.Add("set");

            var hasResultId = parameterNames.Contains("resultId");

            if (hasResultId)
            {
                parameters.Remove(parameters.First(x => x.Contains("resultId")));
            }

            var paramsParameters = parameters.Where(x => x.Contains("Span"));
            var nullableParameters = parameters.Where(x => x.Contains("?"));
            var normalParameters = parameters.Where(x => !x.Contains("?") && !x.Contains("Span"));
            var other = parameterNames.Where(x => x != "resultType" && x != "resultId" && x != "set");

            var cells = glslDoc!.QuerySelectorAll($"p.tableblock:has(strong:contains(\"{opname}\"))");
            var comment = AddDocComment(cells);
            code.AppendLine(comment);
            code
                .Append("public static Instruction AddGLSL")
                .Append(opname)
                .Append("(this SpirvBuffer buffer, IdResultType resultType, int resultId, ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                    .Append("Span<IdRef> refs = [").Append(string.Join(", ", other)).AppendLine("];")
                    .Append("return buffer.AddOpExtInst(")
                        .Append("set, ")
                        .Append(opcode)
                        .Append(", resultId, resultType ")
                        .AppendLine(", refs);")
                .AppendLine("}");
                
            code.AppendLine(comment);
            code
                .Append("public static Instruction InsertGLSL")
                .Append(opname)
                .Append("(this SpirvBuffer buffer, int position, IdResultType resultType, int resultId, ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                    .Append("Span<IdRef> refs = [").Append(string.Join(", ", other)).AppendLine("];")
                    .Append("return buffer.InsertOpExtInst(position, ")
                        .Append("set, ")
                        .Append(opcode)
                        .Append(", resultId, resultType ")
                        .AppendLine(", refs);")
                .AppendLine("}");
        }
    }
}
