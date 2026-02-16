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
                    grammar.Instructions?.AsList()?.AddRange(parsed.Instructions?.AsList() ?? []);
                if (parsed.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> parsedKinds && grammar.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> grammarKinds)
                {
                    foreach (var pk in parsedKinds)
                    {
                        if (grammarKinds.ContainsKey(pk.Key))
                        {
                            grammarKinds[pk.Key].Enumerants?.AsList()?.AddRange(pk.Value.Enumerants?.AsList() ?? []);
                            if (grammarKinds[pk.Key].Category is null || grammarKinds[pk.Key].Category.Length == 0)
                                grammarKinds[pk.Key] = grammarKinds[pk.Key] with { Category = pk.Value.Category };
                        }
                        else
                            grammarKinds[pk.Key] = pk.Value;
                    }
                }

            }
        }
        return grammar;
    }

    public SpirvGrammar PreProcessEnumerants(SpirvGrammar grammar, CancellationToken _)
    {
        if (grammar.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> dict)
        {
            foreach (var opkind in dict.Values)
            {
                if (opkind.Enumerants?.AsList() is List<Enumerant> enumerants && enumerants.Any(e => e.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 }))
                {
                    for (int i = 0; i < enumerants.Count; i++)
                    {
                        var enumerant = enumerants[i];
                        var buffer = new List<(string, string)>(24);
                        if (enumerant.Parameters?.AsList() is List<EnumerantParameter> parameters)
                        {
                            for (int j = 0; j < parameters.Count; j++)
                            {
                                var param = parameters[j];
                                param.Name = param.Name switch
                                {
                                    string s when s.Any(char.IsPunctuation) => LowerFirst(string.Join("", s.Where(char.IsLetterOrDigit))),
                                    null or "" => $"{KindToVariableName(param.Kind)}{j}",
                                    _ => $"parameter{j}"
                                };
                                param.CSType = param.Kind switch
                                {
                                    "LiteralInteger" => "int",
                                    "LiteralContextDependentNumber" => "int",
                                    "LiteralString" => "string",
                                    string s when s.StartsWith("Id") => "int",
                                    string s => dict[s].Category switch
                                    {
                                        "BitEnum" => $"{param.Kind}Mask",
                                        _ => param.Kind
                                    },
                                    _ => throw new NotImplementedException($"Type {param.Kind} not implemented for parameterized flag generation"),
                                };
                                parameters[j] = param;
                            }
                        }
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
        var glslTask = htmlContext.OpenAsync(req => req.Content(grammar.GLSLDoc));
        glslTask.Wait();
        var coreDoc = coreTask.Result;
        var glslDoc = glslTask.Result;

        var builder = new StringBuilder();
        // var buffer = new List<OperandData>(24);
        if (grammar.Instructions?.AsList() is List<InstructionData> instructions)
        {
            var extinst = instructions.First(x => x.OpName == "OpExtInst");
            // Prebuilt for fast lookup
            var tableblocksCore = coreDoc!.QuerySelectorAll($"p.tableblock").ToArray();
            var coreNodesById = new Dictionary<string, IElement>();
            foreach (var tableblock in tableblocksCore)
            {
                var firstNode = tableblock.ChildNodes.FirstOrDefault();
                if (firstNode is IElement element && element.NodeName == "A" && element.Id != null)
                    coreNodesById.Add(element.Id, tableblock);
            }
            var tableblocksGLSL = glslDoc!.QuerySelectorAll($"p.tableblock").ToArray();
            var glslNodesByName = new Dictionary<string, IElement>();
            foreach (var tableblock in tableblocksGLSL)
            {
                var firstNode = tableblock.ChildNodes.FirstOrDefault();
                if (firstNode is IElement element && element.NodeName == "STRONG")
                    glslNodesByName.Add(element.TextContent, tableblock);
            }

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = grammar.Instructions.Value.AsList()![i]!;

                // setup the documentation
                var element = instruction.OpName switch
                {
                    string v when !v.StartsWith("Op") => glslNodesByName.ContainsKey(instruction.OpName.Replace("GLSL", "")) ? glslNodesByName[instruction.OpName.Replace("GLSL", "")] : null,
                    string v when v.Contains("SDSL") => null, // SDSL does not have documentation
                    string => coreNodesById.ContainsKey(instruction.OpName) ? coreNodesById[instruction.OpName] : null,
                };
                if (element is not null)
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
                    instruction.Documentation = builder.ToString();
                }

                if (!instruction.OpName.StartsWith("Op"))
                    instruction.OpName = $"GLSL{instruction.OpName}";

                // A reusable buffer
                var buffer = new List<(string, string)>(24);

                if (instruction.Operands?.AsList() is List<OperandData> operands)
                {
                    if (instruction.OpName.StartsWith("GLSL"))
                    {
                        operands.InsertRange(0, extinst.Operands?.AsList().Where(x => x is not { Kind: "IdRef", Quantifier: "*" } and not { Kind: "LiteralExtInstInteger" }) ?? []);
                    }
                    PreProcessOperands(instruction, grammar.OperandKinds?.AsDictionary()!, buffer);
                }

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