using AngleSharp.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.Json;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator : IIncrementalGenerator
{
    public void GenerateStructs(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {
        var sdslInstructionsData =
            grammarProvider
            .Select(static (grammar, _) => grammar.Instructions ?? new([]));

        context.RegisterImplementationSourceOutput(
            sdslInstructionsData,
            GenerateInstructionStructs
        );

    }

    public static void GenerateInstructionStructs(SourceProductionContext spc, EquatableList<InstructionData> instructions)
    {

        StringBuilder builder = new();
        builder
            .AppendLine("using static Stride.Shaders.Spirv.Specification;")
            .AppendLine()
            .AppendLine("namespace Stride.Shaders.Spirv.Core;")
            .AppendLine()
            .AppendLine();

        foreach (var instruction in instructions)
        {

            builder
            .AppendLine($"public ref struct Inst{instruction.OpName} : IWrapperInstruction")
            .AppendLine("{")
            .AppendLine("public Instruction Inner { get; set; }");
            try
            {
                if (instruction.Operands?.AsList() is List<OperandData> operands && operands.Count > 0)
                {
                    var tmp = 1;
                    foreach (var operand in operands)
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

                        if (operand.IsIndexKnown && operand.Class == "Id" || operand.Class == "BitEnum" || operand.Class == "ValueEnum" || operand.Kind == "LiteralExtInstInteger" || operand.Kind == "LiteralSpecConstantOpInteger")
                        {
                            if (operand.Kind == "LiteralExtInstInteger" || operand.Kind == "LiteralSpecConstantOpInteger")
                            {
                                builder.AppendLine($"public int {fieldName} {{get => Inner.Memory.Span[{tmp}]; set => Inner.Memory.Span[{tmp}] = value;}}");
                                tmp += 1;
                            }
                            else if (operand.Class == "BitEnum")
                            {
                                builder.AppendLine($"public {operand.Kind}Mask {fieldName} {{get => ({operand.Kind}Mask)Inner.Memory.Span[{tmp}]; set => Inner.Memory.Span[{tmp}] = (int)value;}}");
                                tmp += 1;
                            }
                            else if (operand.Class == "ValueEnum")
                            {
                                builder.AppendLine($"public {operand.Kind} {fieldName} {{get => ({operand.Kind})Inner.Memory.Span[{tmp}]; set => Inner.Memory.Span[{tmp}] = (int)value;}}");
                                tmp += 1;
                            }
                            else if (operand.Class == "Id")
                            {
                                builder.AppendLine($"public {operand.Kind} {fieldName} {{get => new {operand.Kind}(Inner.Memory.Span[{tmp}]); set => Inner.Memory.Span[{tmp}] = value;}}");
                                tmp += 1;
                            }
                        }
                        else
                        {
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
            }
            catch (Exception e)
            {
                builder.Append("/*").Append(e.Message).Append(" ").Append(e.StackTrace).AppendLine("*/");
            }


            builder
            .AppendLine()
            .AppendLine($"public Inst{instruction.OpName}(Instruction instruction) => Inner = instruction;")
            .AppendLine($"public static implicit operator Inst{instruction.OpName}(Instruction instruction) => new(instruction);");


            builder
            .AppendLine("}")
            .AppendLine();
        }

        spc.AddSource(
            $"InstructionStructs.gen.cs",
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