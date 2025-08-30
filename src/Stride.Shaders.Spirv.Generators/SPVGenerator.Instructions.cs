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
        .AppendLine("using static Stride.Shaders.Spirv.Specification;")
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

                if (instruction.OpName.StartsWith("OpCopyMemory"))
                    continue;
                body1.Clear();
                body2.Clear();
                body3.Clear();
                body4.Clear();
                if (instruction.OpName.Contains("Constant"))
                {
                    // foreach(var types in ["LiteralInteger", "LiteralFloat", "LiteralBool"])
                    // builder.AppendLine($@"
                    // public struct {instruction.OpName}<{}>



                    // ");
                    continue;
                }
                else if (instruction.OpName.Contains("GLSL"))
                    {
                        var extinst = grammar.Instructions?.AsList().First(x => x.OpName == "OpExtInst") ?? throw new Exception("Could not find OpExtInst instruction");

                        body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
                            .AppendLine("{")
                            .AppendLine("DataIndex = index;");
                        if (instruction.Operands?.AsList() is List<OperandData> operands && extinst.Operands?.AsList() is List<OperandData> extOperands)
                        {
                            var allOperands = extOperands.Concat(operands).Where(x => x is not { Kind: "IdRef", Quantifier: "*" } and not { Kind: "LiteralExtInstInteger" });
                            body2.AppendLine("foreach (var o in index.Buffer[index.Index])")
                            .AppendLine("{");

                            body3.Append($"public {instruction.OpName}(")
                            .Append(string.Join(", ", allOperands.Select(x =>
                            {
                                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(x);
                                return $"{typename} {operandName}";
                            })))
                            .AppendLine(")")
                            .AppendLine("{");


                            // Body 1
                            if (allOperands.Any(x => x is { Kind: "IdResult" }))
                                body1.AppendLine($"public static implicit operator IdRef({instruction.OpName} inst) => new IdRef(inst.ResultId);")
                                .AppendLine($"public static implicit operator int({instruction.OpName} inst) => inst.ResultId;");
                            body1.AppendLine($"public int Instruction => {instruction.OpCode};");
                            foreach (var operand in allOperands)
                            {

                                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

                                if (typename.StartsWith("LiteralArray"))
                                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); UpdateInstructionMemory(); }} }}");
                                else
                                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field = value; UpdateInstructionMemory(); }} }}");

                                // Body 2
                                body2.AppendLine($"if(o.Name == \"{operandName}\")");
                                if (typename.StartsWith("LiteralArray"))
                                    body2.AppendLine($"{fieldName} = o.To<LiteralArray<{operand.Kind}>>();");
                                else body2.AppendLine($"{fieldName} = o.To{(operand.Class?.ToString().Contains("Enum") ?? false ? "Enum" : "")}<{operand.Kind}{(operand.Class is "BitEnum" ? "Mask" : "")}>();");
                                // Body 3
                                if (typename.StartsWith("LiteralArray"))
                                    body3.AppendLine($"{fieldName}.Assign({operandName});");
                                else body3.AppendLine($"{fieldName} = {operandName};");
                            }
                            body2.AppendLine("}");

                            body3.AppendLine("UpdateInstructionMemory();")
                            .AppendLine("}");

                            // Body 4

                            body4.AppendLine("public void UpdateInstructionMemory()")
                            .AppendLine("{")
                            .Append($"Span<int> instruction = [(int)SDSLOp.{extinst.OpName}, ")
                            .Append(string.Join(", ", extOperands.Concat(operands).Where(x => x is not { Kind: "IdRef", Quantifier: "*" }).Select(x =>
                            {
                                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(x);
                                return $".. {fieldName}.AsSpirvSpan()";
                            })))
                            .Append("];")
                            .AppendLine(@"
                        instruction[0] |= instruction.Length << 16;
                        if(instruction.Length == InstructionMemory.Length)
                            instruction.CopyTo(InstructionMemory.Span);
                        else
                        {
                            var tmp = MemoryOwner<int>.Allocate(instruction.Length);
                            instruction.CopyTo(tmp.Span);
                            InstructionMemory?.Dispose();
                            InstructionMemory = tmp;
                        }"
                            )
                            .AppendLine("}");

                        }
                        body2.AppendLine("}");

                    }
                    else
                    {
                        body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
                            .AppendLine("{")
                            .AppendLine("DataIndex = index;");

                        if (instruction.Operands?.AsList() is List<OperandData> operands)
                        {
                            body2.AppendLine("foreach (var o in index.Buffer[index.Index])")
                            .AppendLine("{");

                            body3.Append($"public {instruction.OpName}(")
                            .Append(string.Join(", ", operands.Select(x =>
                            {
                                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(x);
                                return $"{typename} {operandName}";
                            })))
                            .AppendLine(")")
                            .AppendLine("{");


                            // Body 1

                            if (operands.Any(x => x is { Kind: "IdResult" }))
                                body1.AppendLine($"public static implicit operator IdRef({instruction.OpName} inst) => new IdRef(inst.ResultId);")
                                .AppendLine($"public static implicit operator int({instruction.OpName} inst) => inst.ResultId;");
                            foreach (var operand in operands)
                            {

                                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

                                if (typename.StartsWith("LiteralArray"))
                                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); UpdateInstructionMemory(); }} }}");
                                else
                                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field = value; UpdateInstructionMemory(); }} }}");

                                // Body 2
                                body2.AppendLine($"if(o.Name == \"{operandName}\")");
                                if (typename.StartsWith("LiteralArray"))
                                    body2.AppendLine($"{fieldName} = o.To<LiteralArray<{operand.Kind}>>();");
                                else body2.AppendLine($"{fieldName} = o.To{(operand.Class?.ToString().Contains("Enum") ?? false ? "Enum" : "")}<{operand.Kind}{(operand.Class is "BitEnum" ? "Mask" : "")}>();");
                                // Body 3
                                if (typename.StartsWith("LiteralArray"))
                                    body3.AppendLine($"{fieldName}.Assign({operandName});");
                                else body3.AppendLine($"{fieldName} = {operandName};");
                            }
                            body2.AppendLine("}");

                            body3.AppendLine("UpdateInstructionMemory();")
                            .AppendLine("}");

                            // Body 4

                            body4.AppendLine("public void UpdateInstructionMemory()")
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
                        if(instruction.Length == InstructionMemory.Length)
                            instruction.CopyTo(InstructionMemory.Span);
                        else
                        {
                            var tmp = MemoryOwner<int>.Allocate(instruction.Length);
                            instruction.CopyTo(tmp.Span);
                            InstructionMemory?.Dispose();
                            InstructionMemory = tmp;
                        }"
                            )
                            .AppendLine("}");

                        }
                        else
                            body4.AppendLine("public void UpdateInstructionMemory(){}");
                        body2.AppendLine("}");


                    }
                builder.AppendLine($@"
                public struct {instruction.OpName} : IMemoryInstruction
                {{
                    public OpDataIndex? DataIndex {{ get; set; }}
                    public MemoryOwner<int> InstructionMemory
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
        string typename = (operand.Kind, operand.Quantifier, operand.Class) switch
        {
            // ("PairIdRefIdRef", null or "") => "(IdRef, IdRef)",
            // ("PairIdRefLiteralInteger", null or "") => "(IdRef, LiteralInteger)",
            // ("PairLiteralIntegerIdRef", null or "") => "(LiteralInteger, IdRef)",


            (string s, null or "", _) when s.StartsWith("Id") => "int",
            ("LiteralInteger", null or "", _) => "int",
            ("LiteralExtInstInteger", null or "", _) => "int",
            ("LiteralFloat", null or "", _) => "float",
            ("LiteralString", null or "", _) => "LiteralString",
            (string s, null or "", _) when s.StartsWith("Pair") => "(int, int)",
            (string s, null or "", "BitEnum") when !s.StartsWith("Literal") => $"{s}Mask",
            (string s, null or "", "ValueEnum") when !s.StartsWith("Literal") => s,
            (string s, "?", _) when s.StartsWith("Id") => "int?",
            ("LiteralInteger", "?", _) => "int?",
            ("LiteralExtInstInteger", "?", _) => "int?",
            ("LiteralFloat", "?", _) => "float?",
            ("LiteralString", "?", _) => "LiteralString?",
            (string s, "?", "BitEnum") when !s.StartsWith("Literal") => $"{s}Mask?",
            (string s, "?", "ValueEnum") when !s.StartsWith("Literal") => $"{s}?",
            (string s, "*", _) when s.StartsWith("Id") => $"LiteralArray<{s}>",
            ("LiteralInteger", "*", _) => "LiteralArray<LiteralInteger>",
            ("LiteralExtInstInteger", "*", _) => "LiteralArray<LiteralInteger>",
            ("LiteralFloat", "*", _) => "LiteralArray<LiteralFloat>",
            ("LiteralString", "*", _) => "LiteralArray<LiteralString>",
            // (string s, "*") when !s.StartsWith("Literal") => $"LiteralArray<{s}>",
            (string s, "*", _) when s.StartsWith("Pair") => $"LiteralArray<{s}>",
            _ => throw new NotImplementedException($"Could not generate C# type for '{operand.Kind}{operand.Quantifier}'")
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