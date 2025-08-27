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
        // var sdslInstructionsData =
        //     grammarProvider
        //     .Select(static (grammar, _) => grammar.Instructions ?? new([]));

        context.RegisterImplementationSourceOutput(
            grammarProvider,
            GenerateInstructionStructs
        );

    }
    public static void GenerateInstructionStructs(SourceProductionContext spc, SpirvGrammar grammar)
    {
        StringBuilder builder = new();
        builder
        .AppendLine("using static Spv.Specification;")
        .AppendLine("using CommunityToolkit.HighPerformance;")
        .AppendLine("using CommunityToolkit.HighPerformance.Buffers;")
        .AppendLine("using Stride.Shaders.Spirv.Core.Buffers;")
        .AppendLine()
        .AppendLine("namespace Stride.Shaders.Spirv.Core;")
        .AppendLine()
        .AppendLine();

        StringBuilder body1 = new();
        StringBuilder body2 = new();
        StringBuilder body3 = new();
        StringBuilder body4 = new();
        if (grammar.Instructions?.AsList() is List<InstructionData> instructions)
        {
            foreach (var instruction in instructions)
            {



                if (instruction.OpName.StartsWith("OpCopyMemory") || !instruction.OpName.StartsWith("OpType"))
                    continue;
                body1.Clear();
                body2.Clear();
                body3.Clear();
                body4.Clear();

                if (instruction.Operands?.AsList() is List<OperandData> operands)
                {
                    body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
                    .AppendLine("{")
                    .AppendLine("DataIndex = index;")
                    .AppendLine("foreach (var o in index.Buffer[index.Index])")
                    .AppendLine("{");

                    body3.Append($"public {instruction.OpName}(")
                    .Append(string.Join(", ", operands.Select(x =>
                    {
                        (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(x);
                        return $"{x.Kind} {operandName}";
                    })))
                    .AppendLine(")")
                    .AppendLine("{");


                    // Body 1
                    foreach (var operand in operands)
                    {

                        (string type, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);


                        body1.Append($"public {operand.Kind} {fieldName} {{ get; set {{ field = value; UpdateMemory(); }} }}");

                        // Body 2
                        body2.AppendLine($"if(o.Name == \"{operandName}\")")
                        .AppendLine($"{fieldName} = o.To{(operand.Class?.ToString().Contains("Enum") ?? false ? "Enum" : "")}<{operand.Kind}>();");
                        // Body 3
                        body3.AppendLine($"{fieldName} = {operandName};");
                    }
                    body2.AppendLine("}\n}");

                    body3.AppendLine("UpdateMemory();")
                    .AppendLine("}");

                    // Body 4

                    body4.AppendLine("public void UpdateMemory()")
                    .AppendLine("{")
                    .Append($"Span<int> instruction = [(int)SDSLOp.{instruction.OpName}, ")
                    .Append(string.Join(", ", operands.Select(x =>
                    {
                        (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(x);
                        return $".. {fieldName}.AsSpirvSpan()";
                    })))
                    .Append("];")
                    .AppendLine(@"
                        instruction[0] |= instruction.Length << 16;
                        var tmp = MemoryOwner<int>.Allocate(instruction.Length);
                        instruction.CopyTo(tmp.Span);
                        Memory?.Dispose();
                        Memory = tmp;"
                    )
                    .AppendLine("}");

                }


                builder.AppendLine($@"
public struct {instruction.OpName} : IMemoryInstruction
{{
    public OpDataIndex? DataIndex {{ get; set; }}
    public MemoryOwner<int> Memory
    {{
        readonly get
        {{
            if (DataIndex is OpDataIndex odi)
                return odi.Buffer[odi.Index].Memory;
            else return field;
        }}

        private set
        {{
            if (DataIndex is OpDataIndex odi)
            {{
                odi.Buffer[odi.Index].Memory.Dispose();
                odi.Buffer[odi.Index].Memory = value;
            }}
            else field = value;
        }}
    }}

    {body1}
    {body2}
    {body3}
    {body4}

    public static implicit operator {instruction.OpName}(OpDataIndex odi) => new(odi);
}}

                
");
                // builder.AppendLine($"public ref struct {instruction.OpName} : IWrapperInstruction")
                // .AppendLine("{")
                // .AppendLine("public RefInstruction Inner { get; set; }")
                // .AppendLine("public int WordCount => Inner.WordCount;");
                // if (instruction.Operands.AsList() is List<OperandData> operands && operands.Count > 0)
                // {
                //     foreach (var operand in operands)
                //     {
                //         string fieldName;
                //         string operandName = ConvertOperandName(operand.Name ?? ConvertKindToName(operand.Kind), operand.Quantifier);
                //         if (operand.Name is null or "")
                //             fieldName = ConvertKindToName(operand.Kind, false);
                //         else
                //         {
                //             var nameBuilder = new StringBuilder();
                //             bool first = true;
                //             foreach (var c in operand.Name)
                //             {
                //                 if (char.IsLetterOrDigit(c) || c == '_')
                //                 {
                //                     nameBuilder.Append(first ? char.ToUpperInvariant(c) : c);
                //                     first &= false;
                //                 }

                //             }
                //             fieldName = nameBuilder.ToString();
                //         }
                //         if (operand.Kind == "LiteralContextDependentNumber")
                //             continue;
                //         else if (operand.Kind == "LiteralInteger" || operand.Kind == "LiteralExtInstInteger" || operand.Kind == "LiteralSpecConstantOpInteger")
                //             builder.AppendLine($"public LiteralInteger {fieldName} => Inner.GetOperand<LiteralInteger>(\"{operandName}\") ?? default;");
                //         else if (operand.Class == "BitEnum")
                //             builder.AppendLine($"public {operand.Kind}Mask {fieldName} => Inner.GetEnumOperand<{operand.Kind}Mask>(\"{operandName}\");");
                //         else if (operand.Class == "ValueEnum")
                //             builder.AppendLine($"public {operand.Kind} {fieldName} => Inner.GetEnumOperand<{operand.Kind}>(\"{operandName}\");");
                //         else
                //             builder.AppendLine($"public {operand.Kind} {fieldName} => Inner.GetOperand<{operand.Kind}>(\"{operandName}\") ?? default;");
                //     }
                // }


                // builder
                // .AppendLine()
                // .AppendLine($"public {instruction.OpName}(RefInstruction instruction) => Inner = instruction;")
                // .AppendLine($"public {instruction.OpName}(Span<int> buffer) => Inner = RefInstruction.ParseRef(buffer);")
                // .AppendLine($"public static implicit operator IdRef({instruction.OpName} instruction) => new(instruction.Inner.ResultId ?? throw new Exception(\"Instruction has no result id\"));")
                // .AppendLine($"public static implicit operator IdResultType({instruction.OpName} instruction) => new(instruction.Inner.ResultId ?? throw new Exception(\"Instruction has no result id\"));")
                // .AppendLine($"public static implicit operator {instruction.OpName}(Span<int> buffer) => new {instruction.OpName}(buffer);")
                // .AppendLine($"public static implicit operator {instruction.OpName}(Instruction instruction) => new {instruction.OpName}(instruction.AsRef());")
                // .AppendLine($"public static implicit operator {instruction.OpName}(RefInstruction instruction) => new {instruction.OpName}(instruction);");
                // builder.AppendLine("}");

            }
        }
        spc.AddSource(
            $"Instructions.g.cs",
            SourceText.From(
                SyntaxFactory
                .ParseCompilationUnit(builder.ToString())
                .NormalizeWhitespace()
                .ToFullString(),
                Encoding.UTF8
            )
        );
    }

    public static (string TypeName, string FieldName, string OperandName) ToTypeFieldAndOperandName(OperandData operand)
    {
        string typename = (operand.Kind, operand.Quantifier) switch
        {
            // ("PairIdRefIdRef", null or "") => "(IdRef, IdRef)",
            // ("PairIdRefLiteralInteger", null or "") => "(IdRef, LiteralInteger)",
            // ("PairLiteralIntegerIdRef", null or "") => "(LiteralInteger, IdRef)",


            (string s, null or "") when s.StartsWith("Id") => "int",
            ("LiteralInteger", null or "") => "int",
            ("LiteralFloat", null or "") => "float",
            ("LiteralString", null or "") => "string",
            (string s, null or "") when s.StartsWith("Pair") => "(int, int)",
            (string s, null or "") when !s.StartsWith("Literal") => s,
            (string s, "?") when s.StartsWith("Id") => "int?",
            ("LiteralInteger", "?") => "int?",
            ("LiteralFloat", "?") => "float?",
            ("LiteralString", "?") => "string?",
            (string s, "?") when !s.StartsWith("Literal") => $"{s}?",
            (string s, "*") when s.StartsWith("Id") => "int?",
            ("LiteralInteger", "*") => "int[]",
            ("LiteralFloat", "*") => "float[]",
            ("LiteralString", "*") => "string[]",
            (string s, "*") when !s.StartsWith("Literal") => $"{s}?",
            _ => throw new NotImplementedException($"Could not generate C# type for {operand.Kind}-{operand.Quantifier}")
        };


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
        return (typename, fieldName, operandName);
    }
}