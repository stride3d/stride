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
        .AppendLine("using System.Numerics;")
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

                body1.Clear();
                body2.Clear();
                body3.Clear();
                body4.Clear();


                if (instruction.OpName.EndsWith("Constant"))
                    WriteConstantInstructions(grammar, instruction, builder, body1, body2, body3, body4);
                // else if (instruction.OpName.Contains("Decorate"))
                //     WriteDecorateInstructions(grammar, instruction, builder, body1, body2, body3, body4);
                else if (instruction.OpName.StartsWith("OpCopyMemory"))
                    WriteCopyMemoryInstructions(grammar, instruction, body1, body2, body3, body4);
                else if (instruction.OpName.Contains("GLSL"))
                    WriteGLSLCode(grammar, instruction, builder, body1, body2, body3, body4);
                else WriteOtherInstructions(grammar, instruction, builder, body1, body2, body3, body4);
            }
        }
        spc.AddSource(
            $"Instructions.gen.cs",
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
        string typename = (operand.Kind, operand.Quantifier, operand.Class, operand.IsParameterized) switch
        {
            (string s, null or "", "ValueEnum", true) => $"ParameterizedFlag<{s}>",
            (string s, null or "", "BitEnum", true) => $"ParameterizedFlag<{s}Mask>",
            (string s, null or "", _, false) when s.StartsWith("Id") => "int",
            ("LiteralInteger" or "LiteralExtInstInteger" or "LiteralSpecConstantOpInteger", null or "", _, false) => "int",
            ("LiteralFloat", null or "", _, false) => "float",
            ("LiteralString", null or "", _, false) => "string",
            (string s, null or "", _, false) when s.StartsWith("Pair") => "(int, int)",
            (string s, null or "", "BitEnum", false) when !s.StartsWith("Literal") => $"{s}Mask",
            (string s, null or "", "ValueEnum", false) when !s.StartsWith("Literal") => s,
            (string s, "?", "ValueEnum", true) => $"ParameterizedFlag<{s}>?",
            (string s, "?", "BitEnum", true) => $"ParameterizedFlag<{s}Mask>?",
            (string s, "?", _, false) when s.StartsWith("Id") => "int?",
            ("LiteralInteger" or "LiteralExtInstInteger" or "LiteralSpecConstantOpInteger", "?", _, false) => "int?",
            ("LiteralFloat", "?", _, false) => "float?",
            ("LiteralString", "?", _, false) => "string?",
            (string s, "?", "BitEnum", false) when !s.StartsWith("Literal") => $"{s}Mask?",
            (string s, "?", "ValueEnum", false) when !s.StartsWith("Literal") => $"{s}?",
            (string s, "*", _, false) when s.StartsWith("Id") => $"LiteralArray<int>",
            ("LiteralInteger" or "LiteralExtInstInteger" or "LiteralSpecConstantOpInteger", "*", _, false) => "LiteralArray<int>",
            ("LiteralFloat", "*", _, false) => "LiteralArray<float>",
            // ("LiteralString", "*", _) => "LiteralArray<string>",
            (string s, "*", _, false) when s.StartsWith("Pair") => $"LiteralArray<(int, int)>",
            (string s, "*", "BitEnum", false) when !s.StartsWith("Literal") => $"LiteralArray<{s}Mask>",
            (string s, "*", "ValueEnum", false) when !s.StartsWith("Literal") => $"LiteralArray<{s}>",
            ("LiteralContextDependentNumber", null or "", _, false) => "LiteralValue<T>",
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
        return (operand.Class, operand.Quantifier, operand.IsParameterized) switch
        {
            (string s, null or "", false) when s.Contains("Id") => $"{fieldName}",
            (string s, "?", false) when s.Contains("Id") => $".. ({fieldName} is null ? (Span<int>)[] : [{fieldName}.Value])",
            (string s, null or "", false) when s.Contains("Enum") => $"(int){fieldName}",
            (string s, null or "", true) when s.Contains("Enum") => $".. (Span<int>)[(int){fieldName}.Value, .. {fieldName}.Span]",
            (string s, "?", false) when s.Contains("Enum") => $".. ({fieldName} is null ? (Span<int>)[] : [(int){fieldName}.Value])",
            (string s, "?", true) when s.Contains("Enum") => $".. ({fieldName} is null ? (Span<int>)[] : [(int){fieldName}.Value.Value, .. {fieldName}.Value.Span])",
            (string, "*", false) => $".. {fieldName}.Words",
            (string, "?", false) => $".. ({fieldName} is null ? (Span<int>)[] : {fieldName}.AsDisposableLiteralValue().Words)",
            (_, "?", false) => $".. ({fieldName} is null ? (Span<int>)[] : {fieldName}.AsDisposableLiteralValue().Words)",
            _ => $".. {fieldName}.AsDisposableLiteralValue().Words"
        };
    }


    static void WriteOtherInstructions(SpirvGrammar grammar, in InstructionData instruction, StringBuilder builder, StringBuilder body1, StringBuilder body2, StringBuilder body3, StringBuilder body4)
    {
        body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
                                .AppendLine("{");

        if (instruction.Operands?.AsList() is List<OperandData> operands)
        {
            body2.AppendLine("foreach (var o in index.Data)")
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
                body1.AppendLine($"public static implicit operator Id({instruction.OpName} inst) => new Id(inst.ResultId);")
                .AppendLine($"public static implicit operator int({instruction.OpName} inst) => inst.ResultId;");
            var tmp = -1;
            foreach (var operand in operands)
            {
                tmp += 1;
                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

                if (typename.StartsWith("LiteralArray"))
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");

                // Body 2
                body2.AppendLine($"{(tmp == 0 ? "" : "else ")}if(o.Name == \"{operandName}\")");
                bool needCloseBrace = false;
                // Optional operands
                if (operand.Quantifier == "?")
                {
                    body2.AppendLine("{");
                    body2.AppendLine("if (o.Words.Length > 0)");
                    needCloseBrace = true;
                }
                if (typename.StartsWith("LiteralArray"))
                    body2.AppendLine($"{fieldName} = o.To{typename}();");
                else if (operand.Class is string s && s.Contains("Enum"))
                    body2.AppendLine($"{fieldName} = o.ToEnum<{operand.Kind}{(operand.Class is "BitEnum" ? "Mask" : "")}>();");
                else body2.AppendLine($"{fieldName} = o.ToLiteral<{typename.TrimEnd('?')}>();");

                if (needCloseBrace)
                    body2.AppendLine("}");

                if (grammar.OperandKinds?.AsDictionary() is Dictionary<string, OpKind> dict
                    && dict.TryGetValue(operand.Kind, out var opkind) && opkind.Enumerants?.AsList() is List<Enumerant> enumerants && enumerants.Any(x => x.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 }))
                {
                    body2.AppendLine($"else if({string.Join(" || ", enumerants
                        .Where(e => e.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 })
                        .SelectMany(enumerant => enumerant.Parameters?.AsList())
                        .Select(param => $"o.Name == \"{param.Name ?? ConvertKindToName(param.Kind)}\""))})");
                    body2.AppendLine($"{fieldName} = new({fieldName}{(typename.EndsWith("?") ? ".Value" : "")}.Value, o.Words);");
                }

                // Body 3
                body3.AppendLine($"{fieldName} = {operandName};");
            }
            body2.AppendLine("}");

            foreach(var operand in operands.Where(o => o.Quantifier == "*"))
            {
                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);
                body2.AppendLine($"if({fieldName}.WordCount == -1)")
                .AppendLine($"{fieldName} = new();");
            }

            body3
            .AppendLine("UpdateInstructionMemory();")
            .AppendLine("}");

            // Body 4

            body4.AppendLine("public void UpdateInstructionMemory()")
            .AppendLine("{")
            .AppendLine("if(InstructionMemory is null) InstructionMemory = MemoryOwner<int>.Empty;")
            .Append($"Span<int> instruction = [(int)Op.{instruction.OpName}, ")
            .Append(string.Join(", ", operands.Select(ToSpreadOperator)))
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
        body2.AppendLine("DataIndex = index;").AppendLine("}");

        builder.AppendLine($@"
            public struct {instruction.OpName} : IMemoryInstruction
            {{
                public OpDataIndex? DataIndex {{ get; set; }}
                public MemoryOwner<int> InstructionMemory
                {{
                    readonly get
                    {{
                        if (DataIndex is OpDataIndex odi)
                            return odi.Data.Memory;
                        else return field;
                    }}

                    private set
                    {{
                        if (DataIndex is OpDataIndex odi)
                        {{
                            odi.Data.Memory.Dispose();
                            odi.Data.Memory = value;
                        }}
                        else field = value;
                    }}
                }}

                public {instruction.OpName}()
                {{
                    InstructionMemory = MemoryOwner<int>.Allocate(1);
                    InstructionMemory.Span[0] = (int)Op.{instruction.OpName} | (1 << 16);
                }}

                {body1}
                {body2}
                {body3}
                {body4}

                public static implicit operator {instruction.OpName}(OpDataIndex odi) => new(odi);
            }}
        ");
    }
    static void WriteDecorateInstructions(SpirvGrammar grammar, in InstructionData instruction, StringBuilder builder, StringBuilder body1, StringBuilder body2, StringBuilder body3, StringBuilder body4)
    {
        body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
                                .AppendLine("{");

        if (instruction.Operands?.AsList() is List<OperandData> operands)
        {
            if (instruction.OpName.EndsWith("Id"))
                // Note: not sure if this is correct, it might need to be an array (quantifier *) which we don't support nicely yet
                operands.Add(new() { Name = "additionalId", Kind = "IdRef" });
            else if (instruction.OpName.EndsWith("String"))
                operands.Add(new() { Name = "additionalString", Kind = "LiteralString", Quantifier = "?" });
            else
            {
                // Note: not sure if this is correct, it might need to be an array (quantifier *) which we don't support nicely yet
                operands.Add(new() { Name = "additionalInteger", Kind = "LiteralInteger", Quantifier = "?" });
                operands.Add(new() { Name = "additionalInteger2", Kind = "LiteralInteger", Quantifier = "?" });
            }

            body2.AppendLine("foreach (var o in index.Data)")
            .AppendLine("{");

            body3.Append($"public {instruction.OpName}(")
            .Append(string.Join(", ", operands.Select(x =>
            {
                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(x);
                return $"{typename} {operandName} {(typename.EndsWith("?") ? "= null" : "")}";
            })))
            .AppendLine(")")
            .AppendLine("{");


            // Body 1

            if (operands.Any(x => x is { Kind: "IdResult" }))
                body1.AppendLine($"public static implicit operator Id({instruction.OpName} inst) => new Id(inst.ResultId);")
                .AppendLine($"public static implicit operator int({instruction.OpName} inst) => inst.ResultId;");
            var tmp = -1;
            foreach (var operand in operands)
            {
                tmp += 1;
                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

                if (typename.StartsWith("LiteralArray"))
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");

                // Body 2
                if (tmp != 0)
                    body2.Append($"else ");
                body2.AppendLine($"if(o.Name == \"{operandName}\")");
                if (typename.StartsWith("LiteralArray"))
                    body2.AppendLine($"{fieldName} = o.To{typename}();");
                else if (operand.Class is string s && s.Contains("Enum"))
                    body2.AppendLine($"{fieldName} = o.ToEnum<{operand.Kind}{(operand.Class is "BitEnum" ? "Mask" : "")}>();");
                else body2.AppendLine($"{fieldName} = o.ToLiteral<{typename.TrimEnd('?')}>();");
                // Body 3
                if (typename.StartsWith("LiteralArray"))
                    body3.AppendLine($"{fieldName}.Assign({operandName});");
                else body3.AppendLine($"{fieldName} = {operandName};");
            }
            body2.AppendLine("}");

            body3
            .AppendLine("UpdateInstructionMemory();")
            .AppendLine("}");

            // Body 4

            body4.AppendLine("public void UpdateInstructionMemory()")
            .AppendLine("{")
            .AppendLine("if(InstructionMemory is null) InstructionMemory = MemoryOwner<int>.Empty;")
            .Append($"Span<int> instruction = [(int)Op.{instruction.OpName}, ")
            .Append(string.Join(", ", operands.Select(ToSpreadOperator)))
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
        body2.AppendLine("DataIndex = index;").AppendLine("}");

        builder.AppendLine($@"
            public struct {instruction.OpName} : IMemoryInstruction
            {{
                public OpDataIndex? DataIndex {{ get; set; }}
                public MemoryOwner<int> InstructionMemory
                {{
                    readonly get
                    {{
                        if (DataIndex is OpDataIndex odi)
                            return odi.Data.Memory;
                        else return field;
                    }}

                    private set
                    {{
                        if (DataIndex is OpDataIndex odi)
                        {{
                            odi.Data.Memory.Dispose();
                            odi.Data.Memory = value;
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

    static void WriteGLSLCode(SpirvGrammar grammar, in InstructionData instruction, StringBuilder builder, StringBuilder body1, StringBuilder body2, StringBuilder body3, StringBuilder body4)
    {
        var extinst = grammar.Instructions?.AsList().First(x => x.OpName == "OpExtInst") ?? throw new Exception("Could not find OpExtInst instruction");

        body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
            .AppendLine("{");
        if (instruction.Operands?.AsList() is List<OperandData> operands && extinst.Operands?.AsList() is List<OperandData> extOperands)
        {
            var allOperands = extOperands.Concat(operands).Where(x => x is not { Kind: "IdRef", Quantifier: "*" } and not { Kind: "LiteralExtInstInteger" });
            body2.AppendLine("foreach (var o in index.Data)")
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
                body1.AppendLine($"public static implicit operator Id({instruction.OpName} inst) => new Id(inst.ResultId);")
                .AppendLine($"public static implicit operator int({instruction.OpName} inst) => inst.ResultId;");
            body1.AppendLine($"public int Instruction => {instruction.OpCode};");
            foreach (var operand in allOperands)
            {

                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

                if (typename.StartsWith("LiteralArray"))
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");

                // Body 2
                body2.AppendLine($"if(o.Name == \"{operandName}\")");
                if (typename.StartsWith("LiteralArray"))
                {
                    body2.AppendLine($"{fieldName} = o.To{typename}();");
                }
                else if (operand.Class is string s && s.Contains("Enum"))
                    body2.AppendLine($"{fieldName} = o.ToEnum<{operand.Kind}{(operand.Class is "BitEnum" ? "Mask" : "")}>();");
                else body2.AppendLine($"{fieldName} = o.ToLiteral<{typename.TrimEnd('?')}>();");
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
            .AppendLine("if(InstructionMemory is null) InstructionMemory = MemoryOwner<int>.Empty;")
            .Append($"Span<int> instruction = [(int)Op.{extinst.OpName}, ")
            .Append(string.Join(", ", extOperands.Concat(operands).Where(x => x is not { Kind: "IdRef", Quantifier: "*" }).Select(ToSpreadOperator)))
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
        body2.AppendLine("DataIndex = index;").AppendLine("}");
        builder.AppendLine($@"
            public struct {instruction.OpName} : IMemoryInstruction
            {{
                public OpDataIndex? DataIndex {{ get; set; }}
                public MemoryOwner<int> InstructionMemory
                {{
                    readonly get
                    {{
                        if (DataIndex is OpDataIndex odi)
                            return odi.Data.Memory;
                        else return field;
                    }}

                    private set
                    {{
                        if (DataIndex is OpDataIndex odi)
                        {{
                            odi.Data.Memory.Dispose();
                            odi.Data.Memory = value;
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

    static void WriteConstantInstructions(SpirvGrammar grammar, in InstructionData instruction, StringBuilder builder, StringBuilder body1, StringBuilder body2, StringBuilder body3, StringBuilder body4)
    {
        body2.AppendLine($"public {instruction.OpName}(OpDataIndex index)")
                                .AppendLine("{");

        if (instruction.Operands?.AsList() is List<OperandData> operands)
        {
            body2.AppendLine("foreach (var o in index.Data)")
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


            foreach (var operand in operands)
            {

                (string typename, string fieldName, string operandName) = ToTypeFieldAndOperandName(operand);

                if (typename.StartsWith("LiteralArray"))
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field.Assign(value); if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else if (typename.StartsWith("LiteralValue"))
                    body1.Append($"public T {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");
                else
                    body1.Append($"public {typename} {fieldName} {{ get; set {{ field = value; if(InstructionMemory is not null) UpdateInstructionMemory(); }} }}");

                // Body 2
                body2.AppendLine($"if(o.Name == \"{operandName}\")");
                if (typename.StartsWith("LiteralArray"))
                    body2.AppendLine($"{fieldName} = o.To{typename}();");
                else if (operand.Class is string s && s.Contains("Enum"))
                    body2.AppendLine($"{fieldName} = o.ToEnum<{operand.Kind}{(operand.Class is "BitEnum" ? "Mask" : "")}>();");
                else if (typename.StartsWith("LiteralValue"))
                    body2.AppendLine($"{fieldName} = o.ToLiteral<T>();");
                else body2.AppendLine($"{fieldName} = o.ToLiteral<{typename.TrimEnd('?')}>();");
                // Body 3
                if (typename.StartsWith("LiteralArray"))
                    body3.AppendLine($"{fieldName}.Assign({operandName});");
                else body3.AppendLine($"{fieldName} = {operandName};");
            }

            if (operands.Any(x => x is { Kind: "IdResult" }))
                body1.AppendLine($"public static implicit operator Id({instruction.OpName}<T> inst) => new Id(inst.ResultId);")
                .AppendLine($"public static implicit operator int({instruction.OpName}<T> inst) => inst.ResultId;");
            body2.AppendLine("}");

            body3.AppendLine("UpdateInstructionMemory();")
            .AppendLine("}");

            // Body 4

            body4.AppendLine("public void UpdateInstructionMemory()")
            .AppendLine("{")
            .AppendLine("if(InstructionMemory is null) InstructionMemory = MemoryOwner<int>.Empty;")
            .Append($"Span<int> instruction = [(int)Op.{instruction.OpName}, ")
            .Append(string.Join(", ", operands.Select(ToSpreadOperator)))
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
        body2.AppendLine("DataIndex = index;").AppendLine("}");

        builder.AppendLine($@"
        // {string.Join(", ", instruction.Operands?.AsList().Select(x => $"{x.Name}:{x.Kind}"))}
            public struct {instruction.OpName}<T> : IMemoryInstruction
                where T : struct, INumber<T>
            {{
                public OpDataIndex? DataIndex {{ get; set; }}
                public MemoryOwner<int> InstructionMemory
                {{
                    readonly get
                    {{
                        if (DataIndex is OpDataIndex odi)
                            return odi.Data.Memory;
                        else return field;
                    }}

                    private set
                    {{
                        if (DataIndex is OpDataIndex odi)
                        {{
                            odi.Data.Memory.Dispose();
                            odi.Data.Memory = value;
                        }}
                        else field = value;
                    }}
                }}

                {body1}
                {body2}
                {body3}
                {body4}

                public static implicit operator {instruction.OpName}<T>(OpDataIndex odi) => new(odi);
            }}
        ");
    }
    static void WriteCopyMemoryInstructions(SpirvGrammar grammar, in InstructionData instruction, StringBuilder body1, StringBuilder body2, StringBuilder body3, StringBuilder body4)
    {

    }
}