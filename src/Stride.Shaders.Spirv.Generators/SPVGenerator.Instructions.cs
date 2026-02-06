using AngleSharp.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Text;
using System.Text.Json;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator : IIncrementalGenerator
{
    internal class StringBuilderPool
    {
        public static StringBuilderPool Instance { get; } = new();
        readonly ConcurrentBag<StringBuilder> pool = [];
        private StringBuilderPool() { }
        public static StringBuilder Get()
        {
            var sb = Instance.pool.TryTake(out var builder) ? builder : new();
            sb.Clear();
            return sb;
        }
        public static void Return(StringBuilder builder) => Instance.pool.Add(builder);
    }
    public void GenerateStructs(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {
        context.RegisterImplementationSourceOutput(
            grammarProvider,
            GenerateInstructionStructs
        );

    }

    public static void GenerateInstructionStructs(SourceProductionContext spc, SpirvGrammar grammar)
    {
        if (grammar.Instructions?.AsList() is List<InstructionData> instructions)
        {
            spc.AddSource(
                $"Instructions.gen.cs",
                SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit(@$"
                        using static Stride.Shaders.Spirv.Specification;
                        using CommunityToolkit.HighPerformance;
                        using CommunityToolkit.HighPerformance.Buffers;
                        using Stride.Shaders.Spirv.Core.Buffers;
                        using System.Numerics;
                        using System.Runtime.CompilerServices;

                        namespace Stride.Shaders.Spirv.Core;
                        
                        {string.Join("\n", instructions.Select(i => GenerateInstructionStructsSingle(i, grammar)))}
                        
                    ")
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                )
            );
        }
    }
    public static string GenerateInstructionStructsSingle(InstructionData instruction, SpirvGrammar grammar)
    {
        if (instruction.OpName.StartsWith("OpCopyMemory") || instruction.OpName.StartsWith("OpCooperativeMatrixLoad") || instruction.OpName.StartsWith("OpCooperativeMatrixStore"))
            return "";
        var structBuilder = StringBuilderPool.Get();
        // var extinst = Instructions?.AsList().First(x => x.OpName == "OpExtInst") ?? throw new Exception("Could not find OpExtInst instruction");

        if (instruction.OpName.EndsWith("Constant"))
            structBuilder.AppendLine($"public ref partial struct {instruction.OpName}<T> : IMemoryInstruction\n    where T : struct, INumber<T>");
        else
            structBuilder.AppendLine(@$"public ref partial struct {instruction.OpName} : IMemoryInstruction");

        structBuilder.AppendLine(@$"
        {{
            private ref OpData opData;
            public ref OpData OpData => ref opData;
            public MemoryOwner<int> InstructionMemory
            {{
                get
                {{
                    if (!Unsafe.IsNullRef(ref OpData))
                        return OpData.Memory;
                    else
                        return field;
                }}
                private set
                {{
                    if (!Unsafe.IsNullRef(ref OpData))
                    {{
                        OpData.Memory.Dispose();
                        OpData.Memory = value;
                    }}
                    else
                        field = value;
                }}
            }}
            public {instruction.OpName}()
            {{
                InstructionMemory = MemoryOwner<int>.Allocate(1);
                InstructionMemory.Span[0] = (int)Op.{(instruction.OpName.StartsWith("GLSL") ? "OpExtInst" : instruction.OpName)} | (1 << 16);
            }}
        ");



        structBuilder
        .AppendLine(@$"
            public {instruction.OpName}(OpDataIndex index)
            {{
                InitializeProperties(ref index.Data);
                opData = ref index.Data;
            }}
            
            public {instruction.OpName}(ref OpData data)
            {{
                InitializeProperties(ref data);
                opData = ref data;
            }}"
        );

        if (instruction.Operands?.AsList() is List<OperandData> operands)
        {

            foreach (var op in operands)
            {
                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(op);
                if (typename.StartsWith("LiteralArray"))
                    structBuilder.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else if (op.IsParameterized)
                    structBuilder.Append(@$"
                        public {typename} {fieldName} {{ get; set; }}
                        public EnumerantParameters {fieldName}Parameters {{ get; set {{ field.Dispose(); field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}"
                    );
                else if (instruction.OpName.EndsWith("Constant") && typename.StartsWith("LiteralValue"))
                    structBuilder.Append($"public T {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else
                    structBuilder.Append($"public {typename} {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
            }

            if (operands.Any(x => x is { Kind: "IdResult" }))
            {
                structBuilder.AppendLine(@$"
                    public static implicit operator int({instruction.OpName}{(instruction.OpName.EndsWith("Constant") ? "<T>" : "")} inst) => inst.ResultId;"
                );
            }
            structBuilder.AppendLine(@$"
                public {instruction.OpName}({string.Join(", ", operands.Select(ToFunctionParameters))})
                {{
                    {string.Join("\n", operands.Select(ToAssignSimple))}
                    UpdateInstructionMemory();
                    opData = ref Unsafe.NullRef<OpData>();
                }}
                "
            );

        }
        structBuilder.AppendLine(@$"

            public void Attach(OpDataIndex index)
            {{
                opData = ref index.Data;
            }}

            public void UpdateInstructionMemory()
            {{
                InstructionMemory ??= MemoryOwner<int>.Empty;
                Span<int> instruction = [(int)Op.{ToSpreadOperators(instruction, grammar)}];
                instruction[0] |= instruction.Length << 16;
                if(instruction.Length == InstructionMemory.Length)
                    instruction.CopyTo(InstructionMemory.Span);
                else
                {{
                    var tmp = MemoryOwner<int>.Allocate(instruction.Length);
                    instruction.CopyTo(tmp.Span);
                    InstructionMemory?.Dispose();
                    InstructionMemory = tmp;
                }}
            }}
            private void InitializeProperties(ref OpData data)
            {{
                foreach (var o in data)
                {{
                    switch(o.Name)
                    {{
                        {ToAssignSwitchCases(instruction.Operands?.AsList() ?? [], grammar)}
                        // We ignore unrecognized operands
                        default: break;
                    }}
                }}

                {string.Join(
                        "\n",
                        (instruction.Operands?.AsList() ?? [])
                        .Where(x => x.Quantifier == "*")
                        .Select(
                            x =>
                            {
                                var (typename, fieldname, operandname) = ToTypeFieldAndOperandName(x);
                                return $"if({fieldname}.WordCount == -1) {fieldname} = new();";
                            }
                        )
                    )}
            }}
            public static implicit operator {instruction.OpName}{(instruction.OpName.EndsWith("Constant") ? "<T>" : "")}(OpDataIndex odi) => new(odi);");

        if (instruction.Operands?.AsList() is List<OperandData> ops && ops.Any(x => x is { Quantifier: "*" } or { IsParameterized: true }))
        {
            structBuilder.AppendLine(@$"
                public void Dispose()
                {{
                    {string.Join("\n", ops.Where(x => x is { Quantifier: "*" }).Select(ToTypeFieldAndOperandName).Select(t => $"{t.FieldName}.Dispose();"))}
                    {string.Join("\n", ops.Where(x => x is { IsParameterized: true }).Select(ToTypeFieldAndOperandName).Select(t => $"{t.FieldName}Parameters.Dispose();"))}
                }}
            ");
        }
        structBuilder.AppendLine("}");
        StringBuilderPool.Return(structBuilder);
        return structBuilder.ToString();

    }

    public static string ToSpreadOperators(InstructionData instruction, SpirvGrammar grammar)
    {
        // (instruction.OpName.StartsWith("GLSL") ? "OpExtInst" : instruction.OpName)}, {string.Join(", ", (instruction.Operands?.AsList() ?? []).Select(ToSpreadOperator))
        if (instruction.Operands?.AsList() is List<OperandData> operands)
        {
            var sb = StringBuilderPool.Get();
            if (instruction.OpName.StartsWith("GLSL"))
            {
                sb.Append("OpExtInst, ");
                foreach (var operand in operands)
                {
                    if (operand != operands[0])
                        sb.Append(", ");
                    sb.Append(ToSpreadOperator(operand));
                    var (_, fieldname, _) = ToTypeFieldAndOperandName(operand);
                    if (fieldname == "Set")
                        sb.Append($", (int)GLSLOp.{instruction.OpName}");
                }
            }
            else
                sb.Append(instruction.OpName).Append(", ").Append(string.Join(", ", operands.Select(ToSpreadOperator)));
            StringBuilderPool.Return(sb);
            return sb.ToString();
        }
        return instruction.OpName;
    }

    public static string ToAssignSimple(OperandData operand)
    {
        (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);
        return $"{fieldName} = {operandName};{(operand.IsParameterized ? $"\n{fieldName}Parameters = {operandName}Parameters;" : "")}";
    }
    public static string ToAssignSwitchCases(List<OperandData> operands, SpirvGrammar grammar)
    {
        var sb = StringBuilderPool.Get();
        foreach (var operand in operands)
        {
            sb.AppendLine(ToAssignSwitchCase(operand));
            if (operand.IsParameterized)
                break;
        }
        StringBuilderPool.Return(sb);
        return sb.ToString();
    }
    public static string ToAssignSwitchCase(OperandData operand)
    {
        var sb = StringBuilderPool.Get();

        (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

        var isOptional = operand.Quantifier == "?";
        var isArray = typename.StartsWith("LiteralArray");
        var opClass = operand.Class;
        var isParameterized = operand.IsParameterized;
        sb.Append($"case \"{operandName}\" : ");
        if (isOptional)
            sb.AppendLine("if (o.Words.Length > 0){");

        if (isArray)
            sb.AppendLine($"{fieldName} = o.To{typename}();");

        else if (opClass == "ValueEnum" && isParameterized)
            sb.AppendLine(@$"
                    {fieldName} = o.ToEnum<{operand.Kind}>();
                    {fieldName}Parameters = new(data.Memory.Span[(o.Offset+2)..]);
                ");
        else if (opClass == "BitEnum" && isParameterized)
            sb.AppendLine(@$"
                    {fieldName} = o.ToEnum<{operand.Kind}Mask>();
                    if(data.Memory.Span.Length > o.Offset + 1)
                        {fieldName}Parameters = new(data.Memory.Span[(o.Offset+2)..]);
                ");
        else if (opClass == "BitEnum" && !isParameterized)
            sb.AppendLine($"{fieldName} = o.ToEnum<{operand.Kind}Mask>();");
        else if (opClass == "ValueEnum" && !isParameterized)
            sb.AppendLine($"{fieldName} = o.ToEnum<{operand.Kind}>();");
        else
            sb.AppendLine($"{fieldName} = o.ToLiteral<{typename.Replace("?", "")}>();");
        if (isOptional)
            sb.AppendLine("}");
        sb.Append("break;");

        StringBuilderPool.Return(sb);
        return sb.ToString();
    }
    public static string ToFunctionParameters(OperandData operand)
    {
        (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

        return operand.IsParameterized ? $"{typename} {operandName}, EnumerantParameters {operandName}Parameters" : $"{typename} {operandName}";
    }

    public static (string TypeName, string FieldName, string OperandName) ToTypeFieldAndOperandName(OperandData operand)
    {
        string typename = (operand.Kind, operand.Quantifier, operand.Class) switch
        {
            (string s, null or "", _) when s.StartsWith("Id") => "int",
            ("LiteralInteger" or "LiteralExtInstInteger" or "LiteralSpecConstantOpInteger", null or "", _) => "int",
            ("LiteralFloat", null or "", _) => "float",
            ("LiteralString", null or "", _) => "string",
            (string s, null or "", _) when s.StartsWith("Pair") => "(int, int)",
            (string s, null or "", "BitEnum") when !s.StartsWith("Literal") => $"{s}Mask",
            (string s, null or "", "ValueEnum") when !s.StartsWith("Literal") => s,
            (string s, "?", _) when s.StartsWith("Id") => "int?",
            ("LiteralInteger" or "LiteralExtInstInteger" or "LiteralSpecConstantOpInteger", "?", _) => "int?",
            ("LiteralFloat", "?", _) => "float?",
            ("LiteralString", "?", _) => "string?",
            (string s, "?", "BitEnum") when !s.StartsWith("Literal") => $"{s}Mask?",
            (string s, "?", "ValueEnum") when !s.StartsWith("Literal") => $"{s}?",
            (string s, "*", _) when s.StartsWith("Id") => $"LiteralArray<int>",
            ("LiteralInteger" or "LiteralExtInstInteger" or "LiteralSpecConstantOpInteger", "*", _) => "LiteralArray<int>",
            ("LiteralFloat", "*", _) => "LiteralArray<float>",
            // ("LiteralString", "*", _) => "LiteralArray<string>",
            (string s, "*", _) when s.StartsWith("Pair") => $"LiteralArray<(int, int)>",
            (string s, "*", "BitEnum") when !s.StartsWith("Literal") => $"LiteralArray<{s}Mask>",
            (string s, "*", "ValueEnum") when !s.StartsWith("Literal") => $"LiteralArray<{s}>",
            ("LiteralContextDependentNumber", null or "", _) => "LiteralValue<T>",
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


    static string ToSpreadOperator(OperandData operand)
    {
        (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);
        return (operand.Class, operand.Quantifier) switch
        {
            (string s, null or "") when s.Contains("Id") => $"{fieldName}",
            (string s, "?") when s.Contains("Id") => $".. ({fieldName} is null ? (Span<int>)[] : [{fieldName}.Value])",
            (string s, null or "") when s.Contains("Enum") =>
                operand.IsParameterized
                ? $"(int){fieldName}, .. {fieldName}Parameters"
                : $"(int){fieldName}",
            (string s, "?") when s.Contains("Enum") =>
                operand.IsParameterized
                ? $".. ({fieldName} is null ? (Span<int>)[] : [(int){fieldName}.Value, ..{fieldName}Parameters])"
                : $".. ({fieldName} is null ? (Span<int>)[] : [(int){fieldName}.Value])",
            (string, "*") => $".. {fieldName}.Words",
            (string, "?") => $".. ({fieldName} is null ? (Span<int>)[] : {fieldName}.AsDisposableLiteralValue().Words)",
            (_, "?") => $".. ({fieldName} is null ? (Span<int>)[] : {fieldName}.AsDisposableLiteralValue().Words)",
            _ => $".. {fieldName}.AsDisposableLiteralValue().Words"
        };
    }
}