using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.CodeAnalysis;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{
    public static bool IsSpirvSpecification(AdditionalText file)
        =>
        Path.GetFileName(file.Path) switch
        {
            "spirv.core.grammar.json"
            or "spirv.sdsl.grammar-ext.json"
            or "extinst.glsl.std.450.grammar.json"
            or "SPIRV.html"
            or "GLSL.std.450.html" => true,
            _ => false
        };

    public SpirvGrammar PreProcessGrammar(ImmutableArray<AdditionalText> files, CancellationToken _)
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

    public SpirvGrammar PreProcessInstructions(SpirvGrammar grammar, CancellationToken _)
    {
        var operandKinds = grammar.OperandKinds?.AsArray().ToDictionary(x => x.Kind, x => x);
        var config = Configuration.Default.WithDefaultLoader();
        var htmlContext = BrowsingContext.New(config);
        var coreTask = htmlContext.OpenAsync(req => req.Content(grammar.CoreDoc));
        coreTask.Wait();
        var glslTask = htmlContext.OpenAsync(req => req.Content(grammar.CoreDoc));
        glslTask.Wait();
        var coreDoc = coreTask.Result;
        var glslDoc = glslTask.Result;

        var builder = new StringBuilder();
        // var buffer = new List<OperandData>(24);
        if (grammar.Instructions?.AsArray() is InstructionData[] instructions)
        {
            for (int i = 0; i < instructions.Length; i++)
            // foreach (var instruction in grammar.Instructions)
            {
                var instruction = grammar.Instructions.Value.AsArray()![i]!;
                
                // setup the documentation
                    var cells = instruction.OpName switch
                {
                    string v when !v.StartsWith("Op") => glslDoc.QuerySelectorAll($"p.tableblock:has(strong:contains(\"{instruction.OpName.Replace("GLSL", "")}\"))"),
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
                
                if (!instruction.OpName.StartsWith("Op"))
                    instruction.OpName = $"GLSL{instruction.OpName}";

                // A reusable buffer
                var buffer = new List<(string, string)>(24);

                if (instruction.Operands?.AsArray() is OperandData[] operands)
                    PreProcessOperands(instruction, operandKinds!, buffer);

                instructions[i] = instruction;

            }
        }
        return grammar;

    }

    public static string KindToVariableName(string kind)
    {
        return kind switch
        {
            "IdResult" => "result",
            "IdResultType" => "resultType",
            "IdRef" => "idRef",
            _ => kind.Replace("'", "").Replace(" ", "").ToLowerInvariant()
        };
    }
}
