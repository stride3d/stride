using AngleSharp.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.Json;

namespace Stride.Shaders.Spirv.Generators;


public record struct SpirvInstructionData
{
    public string Name { get; }
    public string OpCode { get; }
    public string Category { get; }
    public string Description { get; }
    public EquatableArray<string> Operands { get; }
    public EquatableArray<string> Returns { get; }

    public SpirvInstructionData(string name, string opcode, string category, string description, string[] operands, string[] returns)
    {
        Name = name;
        OpCode = opcode;
        Category = category;
        Description = description;
        Operands = new(operands);
        Returns = new(returns);
    }
}


public partial class SPVGenerator : IIncrementalGenerator
{
    public void GenerateStructs(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<InstructionData> instructionsData =
            context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path) == "spirv.core.grammar.json")
            .Select((file, _) => file.GetText()?.ToString())
            .Where(text => text is not null)
            .Select((text, _) =>
                {
                    var result = JsonSerializer.Deserialize<SpirvGrammar>(text!, options);
                    if (result is SpirvGrammar grammar)
                    {
                        var list = new List<OperandData>(24);
                        var dict = grammar.OperandKinds.ToDictionary(x => x.Kind, x => x.Category);
                        if (grammar.Instructions is List<InstructionData> instructions)
                        {
                            for (int i = 0; i < instructions.Count; i++)
                            {
                                list.Clear();
                                if (instructions[i].Operands is EquatableArray<OperandData> operands)
                                {
                                    foreach (var op in operands)
                                        list.Add(op with { Class = dict[op.Kind] });
                                    instructions[i] = instructions[i] with { Operands = list };
                                }
                            }
                        }
                    }
                    return result;
                })
            .SelectMany((grammar, _) => grammar!.Instructions ?? [])
            .Where(instruction => !instruction.OpName.StartsWith("OpCopyMemory"));

        IncrementalValuesProvider<InstructionData> glslInstructionsData =
            context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path) == "extinst.glsl.std.450.grammar.json" )
            .Select((file, _) => file.GetText()?.ToString())
            .Where(text => text is not null)
            .Select((text, _) =>
                {
                    var result = JsonSerializer.Deserialize<SpirvGrammar>(text!, options);
                    if (result is SpirvGrammar grammar)
                    {
                        var list = new List<OperandData>(24);
                        var dict = spirvCore!.OperandKinds.ToDictionary(x => x.Kind, x => x.Category);
                        if (grammar.Instructions is List<InstructionData> instructions)
                        {
                            for (int i = 0; i < instructions.Count; i++)
                            {
                                list.Clear();
                                if (instructions[i].Operands is EquatableArray<OperandData> operands)
                                {
                                    foreach (var op in operands)
                                        list.Add(op with { Class = dict[op.Kind] });
                                    instructions[i] = instructions[i] with { Operands = list };
                                }
                            }
                        }
                    }
                    return result;
                })
            .SelectMany((grammar, _) => grammar!.Instructions ?? [])
            .Where(instruction => !instruction.OpName.StartsWith("OpCopyMemory"));

        IncrementalValuesProvider<InstructionData> sdslInstructionsData =
            context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path) == "spirv.sdsl.grammar-ext.json")
            .Select((file, _) => file.GetText()?.ToString())
            .Where(text => text is not null)
            .Select((text, _) =>
                {
                    var result = JsonSerializer.Deserialize<SpirvGrammar>(text!, options);
                    if (result is SpirvGrammar grammar)
                    {
                        var list = new List<OperandData>(24);
                        var dict = spirvCore!.OperandKinds.ToDictionary(x => x.Kind, x => x.Category);
                        if (grammar.Instructions is List<InstructionData> instructions)
                        {
                            for (int i = 0; i < instructions.Count; i++ )
                            {
                                list.Clear();
                                if (instructions[i].Operands is EquatableArray<OperandData> operands)
                                {
                                    foreach (var op in operands)
                                        list.Add(op with { Class = dict[op.Kind] });
                                    instructions[i] = instructions[i] with { Operands = list };
                                }
                            }
                        }
                    }
                    return result;
                })
            .SelectMany((grammar, _) => grammar!.Instructions ?? [])
            .Where(instruction => !instruction.OpName.StartsWith("OpCopyMemory"));


        context.RegisterImplementationSourceOutput(instructionsData,
            static (spc, source) => Execute(source, spc));
        context.RegisterImplementationSourceOutput(glslInstructionsData,
            static (spc, source) => Execute(source, spc));
        context.RegisterImplementationSourceOutput(sdslInstructionsData,
            static (spc, source) => Execute(source, spc));

    }

    public static void Execute(InstructionData instruction, SourceProductionContext spc)
    {
        StringBuilder builder = new();
        builder
        .AppendLine("using static Spv.Specification;")
        .AppendLine()
        .AppendLine("namespace Stride.Shaders.Spirv.Core;")
        .AppendLine()
        .AppendLine()
        .AppendLine($"public ref struct Ref{instruction.OpName} : IWrapperInstruction")
        .AppendLine("{")
        .AppendLine("public RefInstruction Inner { get; set; }");
        try
        {
            if (instruction.Operands != null)
            {
                foreach (var operand in instruction.Operands)
                {
                    string fieldName;
                    string operandName = ConvertOperandName(operand.Name ?? ConvertKindToName(operand.Kind), operand.Quantifier);
                    if (operand.Name is null or "")
                        fieldName = ConvertKindToName(operand.Kind, false);
                    else
                    {
                        var nameBuilder = new StringBuilder();
                        bool first = true;
                        foreach (var c in operand.Name)
                        {
                            if (char.IsLetterOrDigit(c) || c == '_')
                            {
                                nameBuilder.Append(first ? char.ToUpperInvariant(c) : c);
                                first &= false;
                            }

                        }
                        fieldName = nameBuilder.ToString();
                    }
                    if (operand.Kind == "LiteralContextDependentNumber")
                        continue;
                    else if (operand.Kind == "LiteralInteger" || operand.Kind == "LiteralExtInstInteger" || operand.Kind == "LiteralSpecConstantOpInteger")
                        builder.AppendLine($"public LiteralInteger {fieldName} => Inner.GetOperand<LiteralInteger>(\"{operandName}\") ?? default;");
                    else if (operand.Class == "BitEnum")
                        builder.AppendLine($"public {operand.Kind}Mask {fieldName} => Inner.GetEnumOperand<{operand.Kind}Mask>(\"{operandName}\");");
                    else if (operand.Class == "ValueEnum")
                        builder.AppendLine($"public {operand.Kind} {fieldName} => Inner.GetEnumOperand<{operand.Kind}>(\"{operandName}\");");
                    else
                        builder.AppendLine($"public {operand.Kind} {fieldName} => Inner.GetOperand<{operand.Kind}>(\"{operandName}\") ?? default;");
                }
            }
        }
        catch (Exception e)
        {
            builder.Append("/*").Append(e.Message).Append(" ").Append(e.StackTrace).AppendLine("*/");
        }


        builder
        .AppendLine()
        .AppendLine($"public Ref{instruction.OpName}(RefInstruction instruction) => Inner = instruction;")
        .AppendLine($"public Ref{instruction.OpName}(Span<int> buffer) => Inner = RefInstruction.ParseRef(buffer);");


        builder.AppendLine("}");
        spc.AddSource(
            $"{instruction.OpName}.Instruction.g.cs",
            SourceText.From(
                SyntaxFactory
                .ParseCompilationUnit(builder.ToString())
                .NormalizeWhitespace()
                .ToFullString(),
                Encoding.UTF8
            )
        );
    }
}