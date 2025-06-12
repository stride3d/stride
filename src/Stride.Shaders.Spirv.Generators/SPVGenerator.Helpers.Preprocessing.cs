using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Common;
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

                    grammar.Instructions?.AsList()?.AddRange(parsed.Instructions?.AsList() ?? []);
                }
                if (parsed.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> parsedKinds && grammar.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> grammarKinds)
                {
                    foreach (var pk in parsedKinds)
                    {
                        if (grammarKinds.ContainsKey(pk.Key))
                        {
                            grammarKinds[pk.Key].Enumerants?.AsList()?.AddRange(pk.Value.Enumerants?.AsList() ?? []);
                            if (grammarKinds[pk.Key].Category is null || grammarKinds[pk.Key].Category.Length == 0)
                                grammarKinds[pk.Key] = grammarKinds[pk.Key] with { Category = pk.Value.Category};
                        }
                        else
                            grammarKinds[pk.Key] = pk.Value;
                    }
                }

            }
        }
        return grammar;
    }

    public SpirvGrammar PreProcessInstructions(SpirvGrammar grammar, CancellationToken _)
    {
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
        if (grammar.Instructions?.AsList() is List<InstructionData> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            // foreach (var instruction in grammar.Instructions)
            {
                var instruction = grammar.Instructions.Value.AsList()![i]!;

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

                if (instruction.Operands?.AsList() is List<OperandData> operands)
                    PreProcessOperands(instruction, grammar.OperandKinds?.AsDictionary()!, buffer);

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
