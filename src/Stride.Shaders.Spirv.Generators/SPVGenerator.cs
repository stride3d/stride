using AngleSharp.Dom;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using AngleSharp;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Stride.Shaders.Spirv.Generators;


[Generator]
public partial class SPVGenerator : IIncrementalGenerator
{

    // Dictionary<string, OpKind> operandKinds = [];

    static readonly JsonSerializerOptions options = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // #if DEBUG
        //             if (!Debugger.IsAttached)
        //                 Debugger.Launch();
        // #endif
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<OperandData>))
            options.Converters.Add(new EquatableArrayJsonConverter<OperandData>());
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<InstructionData>))
            options.Converters.Add(new EquatableArrayJsonConverter<InstructionData>());
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<OpKind>))
            options.Converters.Add(new EquatableArrayJsonConverter<OpKind>());

        // spirvCore = JsonSerializer.Deserialize<SpirvGrammar>(new StreamReader(assembly.GetManifestResourceStream(resourceCoreName)).ReadToEnd(), options);
        // spirvGlsl = JsonSerializer.Deserialize<SpirvGrammar>(new StreamReader(assembly.GetManifestResourceStream(resourceGlslName)).ReadToEnd(), options);
        // spirvSDSL = JsonSerializer.Deserialize<SpirvGrammar>(new StreamReader(assembly.GetManifestResourceStream(resourceSDSLName)).ReadToEnd(), options);


        // var config = Configuration.Default.WithDefaultLoader();
        // var htmlContext = BrowsingContext.New(config);
        // var documentTask = htmlContext.OpenAsync(req => req.Content(new StreamReader(assembly.GetManifestResourceStream(resourceUnifiedName)).ReadToEnd()));
        // documentTask.Wait();
        // unifiedDoc = documentTask.Result;
        // var glslDocumentTask = htmlContext.OpenAsync(req => req.Content(new StreamReader(assembly.GetManifestResourceStream(resourceGlslRegistryName)).ReadToEnd()));
        // glslDocumentTask.Wait();
        // glslDoc = glslDocumentTask.Result;

        // foreach (var o in spirvCore!.OperandKinds)
        //     operandKinds[o.Kind] = o;




        var grammarData =
            context
            .AdditionalTextsProvider
            .Where(
                static file =>
                Path.GetFileName(file.Path) switch
                {
                    "spirv.core.grammar.json"
                    or "spirv.sdsl.grammar-ext.json"
                    or "extinst.glsl.std.450.grammar.json"
                    or "SPIRV.html"
                    or "GLSL.std.450.html" => true,
                    _ => false
                }
            )
            .Collect()
            .Select(
                static (files, _) =>
                {
                    SpirvGrammar grammar = new();
                    foreach (var file in files)
                    {
                        if (Path.GetFileName(file.Path) == "SPIRV.html")
                            grammar.CoreDoc = file.GetText()?.ToString() ?? "";
                        else if (Path.GetFileName(file.Path) == "GLSL.std.450.html")
                            grammar.GLSLDoc = file.GetText()?.ToString() ?? "";
                        else
                        {
                            var parsed = JsonSerializer.Deserialize<SpirvGrammar>(file.GetText()?.ToString() ?? "{}", options);
                            if (grammar.MagicNumber == "" && parsed.MagicNumber != "")
                            {
                                grammar.MagicNumber = parsed!.MagicNumber;
                                grammar.MajorVersion = parsed.MajorVersion;
                                grammar.MinorVersion = parsed.MinorVersion;
                                grammar.Revision = parsed.Revision;
                            }
                            if (parsed.Instructions is not null)
                            {
                                InstructionData[] instructions = [.. grammar.Instructions, .. parsed.Instructions];
                                grammar.Instructions = instructions;
                            }
                            if (parsed.OperandKinds is not null)
                            {
                                OpKind[] operandKinds = [.. grammar.OperandKinds, .. parsed.OperandKinds];
                                grammar.OperandKinds = operandKinds;
                            }

                        }
                    }
                    return grammar;
                }
            )
            .Select(static (grammar, ct) =>
            {
                var operandKinds = grammar.OperandKinds?.AsArray().ToDictionary(x => x.Kind, x => x.Category);
                var config = Configuration.Default.WithDefaultLoader();
                var htmlContext = BrowsingContext.New(config);
                var coreTask = htmlContext.OpenAsync(req => req.Content(grammar.CoreDoc));
                coreTask.Wait();
                var glslTask = htmlContext.OpenAsync(req => req.Content(grammar.CoreDoc));
                glslTask.Wait();
                var coreDoc = coreTask.Result;
                var glslDoc = glslTask.Result;

                var builder = new StringBuilder();
                var buffer = new List<OperandData>(24);
                for (int i = 0; i < grammar.Instructions?.AsArray()!.Length; i++)
                // foreach (var instruction in grammar.Instructions)
                {
                    var instruction = grammar.Instructions.Value.AsArray()![i]!;
                    // setup the documentation
                    var cells = instruction.OpName switch
                    {
                        string v when v.Contains("GLSL") => glslDoc.QuerySelectorAll($"p.tableblock:has(strong:contains(\"{instruction.OpName}\"))"),
                        string v when v.Contains("SDSL") => null, // SDSL does not have documentation
                        string => coreDoc!.QuerySelectorAll($"p.tableblock:has(#{instruction.OpName})"),
                    };
                    if (cells is not null)
                    {
                        if (cells.FirstOrDefault() is IElement element)
                        {
                            var split = element.TextContent.Split('\n');
                            builder.Clear();
                            builder.AppendLine("/// <summary>").Append("/// <para><c>").Append(split[0]).AppendLine("</c></para>");
                            foreach (var t in split.Skip(1))
                                if (!string.IsNullOrEmpty(t))
                                    builder.Append("/// <para>")
                                        .Append(t.Replace("<id>", "<c>id</c>"))
                                        .AppendLine("</para>");
                            builder.AppendLine("/// </summary>");
                        }
                        instruction.Documentation = builder.ToString();
                    }


                    // setup the operand class
                    buffer.Clear();
                    if (instruction.Operands?.AsArray() is OperandData[] operands)
                    {
                        foreach (var op in operands)
                        {
                            var name = GenerateVariableName(op);
                            var type = GenerateTypeName(op);
                            buffer.Add(op with { Class = operandKinds[op.Kind], Name = name, TypeName = type });
                        }
                        instruction.Operands = buffer;
                    }

                    grammar.Instructions.Value.AsArray()![i] = instruction;

                }
                return grammar;
            }
            );

        CreateInfo(context, grammarData);
        CreateSDSLOp(context, grammarData);
        GenerateStructs(context, grammarData);

        context.RegisterImplementationSourceOutput(
            grammarData,
            static (spc, source) =>
            {
                var operandKinds = source.OperandKinds!.Value.AsArray().ToDictionary(x => x.Kind, x => x);
                var code = new StringBuilder();
                code
                    .AppendLine("using static Spv.Specification;")
                    .AppendLine("using Stride.Shaders.Spirv.Core.Buffers;")
                    .AppendLine("")
                    .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                    .AppendLine("")
                    .AppendLine("public static class SpirvBufferExtensions")
                    .AppendLine("{");
                foreach (var instruction in source.Instructions!.Value.AsArray()!)
                {
                    if (instruction.OpName.StartsWith("Op"))
                        CreateOperation(instruction, code, operandKinds);
                    else
                        CreateGlslOperation(instruction, code, operandKinds);
                }
                code
                    .AppendLine("}");

                spc.AddSource("SpirvBufferExtensions.gen.cs",
                    SourceText.From(
                        SyntaxFactory
                        .ParseCompilationUnit(code.ToString())
                        .NormalizeWhitespace()
                        .ToFullString(),
                        Encoding.UTF8
                    )
                );

            }
        );


    }
}
