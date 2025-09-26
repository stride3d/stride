using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Tools;

public static partial class Spv
{
    public static string Dis(NewSpirvBuffer buffer, bool useNames = true, bool writeToConsole = true)
    {
        var writer = new DisWriter(buffer, useNames, writeToConsole);
        writer.Disassemble();
        writer.ToString();
        return writer.ToString();
    }

    public static string Dis(SpirvReader reader, bool useNames = true, bool writeToConsole = true)
    {
        using var buffer = new NewSpirvBuffer(reader.Words);
        var writer = new DisWriter(buffer, useNames, writeToConsole);
        writer.Disassemble();
        return writer.ToString();
    }

    struct DisWriter(NewSpirvBuffer buffer, bool useNames = true, bool writeToConsole = true)
    {
        DisData data = new(buffer, useNames, writeToConsole);
        readonly StringBuilder builder = new();

        readonly DisWriter AppendLine(string text, ConsoleColor? color = null)
        {
            if (color is not null)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                Console.WriteLine(text);
                Console.ForegroundColor = previousColor;
            }
            else
                Console.WriteLine(text);
            builder.AppendLine(text);
            return this;
        }
        readonly DisWriter Append<T>(T text, ConsoleColor? color = null)
        {
            if (color is not null)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                Console.Write(text);
                Console.ForegroundColor = previousColor;
            }
            else
                Console.Write(text);
            builder.Append(text);
            return this;
        }

        readonly DisWriter AppendIdRef(int id, bool useNames = true)
        {
            if (data.UseNames && useNames && data.NameTable.TryGetValue(id, out var name))
                return Append($"%{name} ", ConsoleColor.Green);
            else return Append($"%{id} ", ConsoleColor.Green);
        }
        readonly DisWriter AppendIdRefs(Span<int> ids)
        {
            foreach (var id in ids)
                AppendIdRef(id);
            return this;
        }
        readonly DisWriter AppendRepeatChar(char c, int count)
        {
            if (data.WriteToConsole)
                for (int i = 0; i < count; i++)
                    Console.Write(c);
            if (count > 0)
                builder.Append(c, count);
            return this;
        }
        readonly DisWriter AppendLiteralNumber<T>(T value)
            where T : struct, INumber<T>
        {
            Append(value, ConsoleColor.Red).Append(' ');
            return this;
        }
        readonly DisWriter AppendLiteralNumbers<T>(Span<int> words)
            where T : struct, INumber<T>
        {
            using var tmp = LiteralArray<T>.From(words);
            foreach (var value in tmp)
                Append(value, ConsoleColor.Red).Append(' ');
            return this;
        }
        readonly DisWriter AppendEnums<T>(SpvOperand operand)
            where T : Enum
        {
            foreach (ref var value in operand.Words)
                Append(Unsafe.As<int, T>(ref value).ToString(), ConsoleColor.Yellow).Append(' ');
            return this;
        }

        readonly DisWriter AppendLiteralString(LiteralValue<string> value, bool dispose = true)
        {
            Append('"', ConsoleColor.Green).Append(value.Value, ConsoleColor.Green).Append('"', ConsoleColor.Green).Append(' ');
            if (dispose)
                value.Dispose();
            return this;
        }
        readonly DisWriter AppendLiteralString(string value)
        {
            Append('"', ConsoleColor.Green).Append(value, ConsoleColor.Green).Append('"', ConsoleColor.Green).Append(' ');
            return this;
        }


        readonly DisWriter AppendContextDependentNumber(SpvOperand operand, OpData data, NewSpirvBuffer buffer)
        {
            int typeId = data.Op switch
            {
                Op.OpConstant or Op.OpSpecConstant => data.Memory.Span[1],
                _ => throw new Exception("Unsupported context dependent number in instruction " + data.Op)
            };
            if (buffer.TryGetInstructionById(typeId, out var typeInst))
            {
                if (typeInst.Op == Op.OpTypeInt)
                {
                    var type = (OpTypeInt)typeInst;
                    _ = type switch
                    {
                        { Width: <= 32, Signedness: 0 } => AppendLiteralNumber(operand.ToLiteral<uint>()),
                        { Width: <= 32, Signedness: 1 } => AppendLiteralNumber(operand.ToLiteral<int>()),
                        { Width: 64, Signedness: 0 } => AppendLiteralNumber(operand.ToLiteral<ulong>()),
                        { Width: 64, Signedness: 1 } => AppendLiteralNumber(operand.ToLiteral<long>()),
                        _ => throw new NotImplementedException("Unsupported int width " + type.Width),
                    };
                }
                else if (typeInst.Op == Op.OpTypeFloat)
                {
                    var type = new OpTypeFloat(typeInst);
                    _ = type switch
                    {
                        { Width: 16 } => AppendLiteralNumber(operand.ToLiteral<Half>()),
                        { Width: 32 } => AppendLiteralNumber(operand.ToLiteral<float>()),
                        { Width: 64 } => AppendLiteralNumber(operand.ToLiteral<double>()),
                        _ => throw new NotImplementedException("Unsupported float width " + type.Width),
                    };
                }
                else
                    throw new NotImplementedException("Unsupported context dependent number with type " + typeInst.Op);
                return this;
            }
            else
                throw new Exception("Cannot find type instruction for id " + typeId);

        }

        readonly DisWriter AppendResultId(int? id = null)
        {
            if (id is int i)
            {
                if (data.UseNames && data.NameTable.TryGetValue(i, out var name))
                {
                    AppendRepeatChar(' ', data.IdOffset - name.Length - 1 - 3);
                    Append('%', ConsoleColor.Cyan);
                    Append(name, ConsoleColor.Cyan);
                }
                else
                {
                    var size = 0;
                    var tmp = i;
                    do
                    {
                        size += 1;
                        tmp /= 10;
                    } while (tmp > 0);
                    AppendRepeatChar(' ', data.IdOffset - size - 1 - 3);
                    Append($"%{i}", ConsoleColor.Cyan);
                }
                Append(" = ");
            }
            else
                AppendRepeatChar(' ', data.IdOffset);
            return this;

        }

        public void Disassemble()
        {
            DisHeader();
            foreach (var instruction in data)
            {
                if (instruction.Op == Op.OpName)
                {
                    var nameInst = (OpName)instruction;
                    data.NameTable[nameInst.Target] = nameInst.Name;
                }
                else if (instruction.Op == Op.OpMemberName)
                {
                    var memberInst = (OpMemberName)instruction;
                    data.NameTable[memberInst.Type + memberInst.Member] = memberInst.Name;
                }
            }
            foreach (var instruction in data)
            {
                DisInstruction(instruction, this);
            }
        }

        public readonly void DisHeader()
        {
            var header = data.Buffer.Header;
            AppendLine($"; SPIR-V");
            AppendLine($"; Version: {header.VersionNumber >> 16}.{header.VersionNumber & 0xFF}");
            AppendLine($"; Generator: {header.Generator}");
            AppendLine($"; Bound: {header.Bound}");
            AppendLine($"; Schema: {header.Schema}");
            AppendLine("");
        }

        public readonly void DisInstruction(in OpDataIndex instruction, in DisWriter writer)
        {
            if (instruction.Op == Op.OpName)
            {
                var nameInst = (OpName)instruction;
                AppendResultId();
                Append("OpName ", ConsoleColor.Blue).AppendIdRef(nameInst.Target).AppendLiteralString(nameInst.Name).AppendLine("");
            }
            else if (instruction.Op == Op.OpMemberName)
            {
                var memberInst = (OpMemberName)instruction;
                AppendResultId();
                Append("OpMemberName ", ConsoleColor.Blue).AppendIdRef(memberInst.Type).AppendLiteralNumber(memberInst.Member).AppendLiteralString(memberInst.Name).AppendLine("");
            }
            else
            {
                ref var data = ref instruction.Data;
                var info = InstructionInfo.GetInfo(data);
                if (info.GetResultIndex(out int resultIndex))
                    AppendResultId(data.Memory.Span[1 + resultIndex]);
                else
                    AppendResultId();
                Append(instruction.Op.ToString(), ConsoleColor.Blue).Append(' ');
                foreach (var operand in data)
                {

                    _ = operand.Kind switch
                    {
                        OperandKind.IdResult => Append(""),
                        OperandKind.LiteralInteger
                        or OperandKind.LiteralExtInstInteger
                        or OperandKind.LiteralSpecConstantOpInteger
                            => (operand.Quantifier, operand.Words.Length) switch
                            {
                                (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => AppendLiteralNumber(operand.ToLiteral<int>()),
                                (OperandQuantifier.ZeroOrMore, _) => AppendLiteralNumbers<int>(operand.Words),
                                _ => throw new NotImplementedException("Unsupported literal integer quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                            },
                        OperandKind.LiteralContextDependentNumber => AppendContextDependentNumber(operand, data, buffer),
                        OperandKind.IdRef or OperandKind.IdResultType =>
                            (operand.Quantifier, operand.Words.Length) switch
                            {
                                (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => AppendIdRef(operand.ToLiteral<int>()),
                                (OperandQuantifier.ZeroOrMore, > 0) => AppendIdRefs(operand.Words),
                                (OperandQuantifier.ZeroOrMore or OperandQuantifier.ZeroOrOne, 0) => Append(""),
                                _ => throw new NotImplementedException("Unsupported id ref quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                            },
                        OperandKind.LiteralFloat => AppendLiteralNumber(operand.ToLiteral<float>()),
                        OperandKind.LiteralString => AppendLiteralString(operand.ToLiteral<string>()),
                        OperandKind.ImageOperands => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<ImageOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<ImageOperandsMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FPFastMathMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FPFastMathModeMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FPFastMathModeMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.SelectionControl => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<SelectionControlMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<SelectionControlMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.LoopControl => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<LoopControlMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<LoopControlMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FunctionControl => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FunctionControlMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FunctionControlMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.MemorySemantics => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<MemorySemanticsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<MemorySemanticsMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.MemoryAccess => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<MemoryAccessMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<MemoryAccessMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.KernelProfilingInfo => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<KernelProfilingInfoMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<KernelProfilingInfoMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.RayFlags => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<RayFlagsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<RayFlagsMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FragmentShadingRate => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FragmentShadingRateMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FragmentShadingRateMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.RawAccessChainOperands => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<RawAccessChainOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<RawAccessChainOperandsMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.SourceLanguage => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<SourceLanguage>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<SourceLanguage>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.ExecutionModel => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<ExecutionModel>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<ExecutionModel>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.AddressingModel => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<AddressingModel>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<AddressingModel>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.MemoryModel => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<MemoryModel>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<MemoryModel>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.ExecutionMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<ExecutionMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<ExecutionMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.StorageClass => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<Specification.StorageClass>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<Specification.StorageClass>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.Dim => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<Dim>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<Dim>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.SamplerAddressingMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<SamplerAddressingMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<SamplerAddressingMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.SamplerFilterMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<SamplerFilterMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<SamplerFilterMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.ImageFormat => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<ImageFormat>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<ImageFormat>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.ImageChannelOrder => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<ImageChannelOrder>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<ImageChannelOrder>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.ImageChannelDataType => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<ImageChannelDataType>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<ImageChannelDataType>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FPRoundingMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FPRoundingMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FPRoundingMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FPDenormMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FPDenormMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FPDenormMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.QuantizationModes => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<QuantizationModes>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<QuantizationModes>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FPOperationMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FPOperationMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FPOperationMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.OverflowModes => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<OverflowModes>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<OverflowModes>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.LinkageType => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<LinkageType>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<LinkageType>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.AccessQualifier => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<AccessQualifier>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<AccessQualifier>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.HostAccessQualifier => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<HostAccessQualifier>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<HostAccessQualifier>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FunctionParameterAttribute => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FunctionParameterAttribute>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FunctionParameterAttribute>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.Decoration => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<Decoration>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<Decoration>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.BuiltIn => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<BuiltIn>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<BuiltIn>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.Scope => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<Scope>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<Scope>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.GroupOperation => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<GroupOperation>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<GroupOperation>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.KernelEnqueueFlags => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<KernelEnqueueFlags>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<KernelEnqueueFlags>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.Capability => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<Capability>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<Capability>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.RayQueryIntersection => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<RayQueryIntersection>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<RayQueryIntersection>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.RayQueryCommittedIntersectionType => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<RayQueryCommittedIntersectionType>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<RayQueryCommittedIntersectionType>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.RayQueryCandidateIntersectionType => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<RayQueryCandidateIntersectionType>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<RayQueryCandidateIntersectionType>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.PackedVectorFormat => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<PackedVectorFormat>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<PackedVectorFormat>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.CooperativeMatrixOperands => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<CooperativeMatrixOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<CooperativeMatrixOperandsMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.CooperativeMatrixLayout => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<CooperativeMatrixLayout>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<CooperativeMatrixLayout>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.CooperativeMatrixUse => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<CooperativeMatrixUse>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<CooperativeMatrixUse>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.CooperativeMatrixReduce => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<CooperativeMatrixReduceMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<CooperativeMatrixReduceMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.TensorClampMode => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<TensorClampMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<TensorClampMode>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.TensorAddressingOperands => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<TensorAddressingOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<TensorAddressingOperandsMask>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.InitializationModeQualifier => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<InitializationModeQualifier>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<InitializationModeQualifier>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.LoadCacheControl => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<LoadCacheControl>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<LoadCacheControl>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.StoreCacheControl => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<StoreCacheControl>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<StoreCacheControl>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.NamedMaximumNumberOfRegisters => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<NamedMaximumNumberOfRegisters>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<NamedMaximumNumberOfRegisters>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        OperandKind.FPEncoding => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.ToEnum<FPEncoding>().ToString(), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums<FPEncoding>(operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        },
                        _ => throw new Exception($"Unhandled operand kind {operand.Kind} with quantifier {operand.Quantifier}"),
                    };
                    // _ = (operand.Kind, operand.Quantifier) switch
                    // {
                    //     (OperandKind.IdResult, _) => Append(""),
                    //     (
                    //         OperandKind.LiteralInteger
                    //         or OperandKind.LiteralExtInstInteger
                    //         or OperandKind.LiteralSpecConstantOpInteger,
                    //         OperandQuantifier.One
                    //     ) => AppendLiteralNumber(operand.ToLiteral<int>()),
                    //     (OperandKind.LiteralContextDependentNumber, OperandQuantifier.One) => AppendContextDependentNumber(operand, data, buffer),
                    //     (OperandKind.IdRef or OperandKind.IdResultType, OperandQuantifier.One) => AppendIdRef(operand.ToLiteral<int>()),
                    //     (OperandKind.IdRef or OperandKind.IdResultType, OperandQuantifier.ZeroOrMore) => AppendIdRefs(operand.Words),
                    //     (OperandKind.LiteralFloat, OperandQuantifier.One) => AppendLiteralNumber(operand.ToLiteral<float>()),
                    //     (OperandKind.LiteralString, OperandQuantifier.One) => AppendLiteralString(operand.ToLiteral<string>()),
                    //     (OperandKind.ImageOperands, OperandQuantifier.One) => Append(operand.ToEnum<ImageOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FPFastMathMode, OperandQuantifier.One) => Append(operand.ToEnum<FPFastMathModeMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.SelectionControl, OperandQuantifier.One) => Append(operand.ToEnum<SelectionControlMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.LoopControl, OperandQuantifier.One) => Append(operand.ToEnum<LoopControlMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FunctionControl, OperandQuantifier.One) => Append(operand.ToEnum<FunctionControlMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.MemorySemantics, OperandQuantifier.One) => Append(operand.ToEnum<MemorySemanticsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.MemoryAccess, OperandQuantifier.One or OperandQuantifier.ZeroOrOne) => Append(operand.ToEnum<MemoryAccessMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.KernelProfilingInfo, OperandQuantifier.One) => Append(operand.ToEnum<KernelProfilingInfoMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.RayFlags, OperandQuantifier.One) => Append(operand.ToEnum<RayFlagsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FragmentShadingRate, OperandQuantifier.One) => Append(operand.ToEnum<FragmentShadingRateMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.RawAccessChainOperands, OperandQuantifier.One) => Append(operand.ToEnum<RawAccessChainOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.SourceLanguage, OperandQuantifier.One) => Append(operand.ToEnum<SourceLanguage>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.ExecutionModel, OperandQuantifier.One) => Append(operand.ToEnum<ExecutionModel>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.AddressingModel, OperandQuantifier.One) => Append(operand.ToEnum<AddressingModel>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.MemoryModel, OperandQuantifier.One) => Append(operand.ToEnum<MemoryModel>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.ExecutionMode, OperandQuantifier.One) => Append(operand.ToEnum<ExecutionMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.StorageClass, OperandQuantifier.One) => Append(operand.ToEnum<Specification.StorageClass>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.Dim, OperandQuantifier.One) => Append(operand.ToEnum<Dim>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.SamplerAddressingMode, OperandQuantifier.One) => Append(operand.ToEnum<SamplerAddressingMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.SamplerFilterMode, OperandQuantifier.One) => Append(operand.ToEnum<SamplerFilterMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.ImageFormat, OperandQuantifier.One) => Append(operand.ToEnum<ImageFormat>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.ImageChannelOrder, OperandQuantifier.One) => Append(operand.ToEnum<ImageChannelOrder>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.ImageChannelDataType, OperandQuantifier.One) => Append(operand.ToEnum<ImageChannelDataType>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FPRoundingMode, OperandQuantifier.One) => Append(operand.ToEnum<FPRoundingMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FPDenormMode, OperandQuantifier.One) => Append(operand.ToEnum<FPDenormMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.QuantizationModes, OperandQuantifier.One) => Append(operand.ToEnum<QuantizationModes>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FPOperationMode, OperandQuantifier.One) => Append(operand.ToEnum<FPOperationMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.OverflowModes, OperandQuantifier.One) => Append(operand.ToEnum<OverflowModes>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.LinkageType, OperandQuantifier.One) => Append(operand.ToEnum<LinkageType>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.AccessQualifier, OperandQuantifier.One) => Append(operand.ToEnum<AccessQualifier>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.HostAccessQualifier, OperandQuantifier.One) => Append(operand.ToEnum<HostAccessQualifier>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FunctionParameterAttribute, OperandQuantifier.One) => Append(operand.ToEnum<FunctionParameterAttribute>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.Decoration, OperandQuantifier.One) => Append(operand.ToEnum<Decoration>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.BuiltIn, OperandQuantifier.One) => Append(operand.ToEnum<BuiltIn>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.Scope, OperandQuantifier.One) => Append(operand.ToEnum<Scope>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.GroupOperation, OperandQuantifier.One) => Append(operand.ToEnum<GroupOperation>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.KernelEnqueueFlags, OperandQuantifier.One) => Append(operand.ToEnum<KernelEnqueueFlags>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.Capability, OperandQuantifier.One) => Append(operand.ToEnum<Capability>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.RayQueryIntersection, OperandQuantifier.One) => Append(operand.ToEnum<RayQueryIntersection>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.RayQueryCommittedIntersectionType, OperandQuantifier.One) => Append(operand.ToEnum<RayQueryCommittedIntersectionType>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.RayQueryCandidateIntersectionType, OperandQuantifier.One) => Append(operand.ToEnum<RayQueryCandidateIntersectionType>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.PackedVectorFormat, OperandQuantifier.One) => Append(operand.ToEnum<PackedVectorFormat>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.CooperativeMatrixOperands, OperandQuantifier.One) => Append(operand.ToEnum<CooperativeMatrixOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.CooperativeMatrixLayout, OperandQuantifier.One) => Append(operand.ToEnum<CooperativeMatrixLayout>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.CooperativeMatrixUse, OperandQuantifier.One) => Append(operand.ToEnum<CooperativeMatrixUse>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.CooperativeMatrixReduce, OperandQuantifier.One) => Append(operand.ToEnum<CooperativeMatrixReduceMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.TensorClampMode, OperandQuantifier.One) => Append(operand.ToEnum<TensorClampMode>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.TensorAddressingOperands, OperandQuantifier.One) => Append(operand.ToEnum<TensorAddressingOperandsMask>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.InitializationModeQualifier, OperandQuantifier.One) => Append(operand.ToEnum<InitializationModeQualifier>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.LoadCacheControl, OperandQuantifier.One) => Append(operand.ToEnum<LoadCacheControl>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.StoreCacheControl, OperandQuantifier.One) => Append(operand.ToEnum<StoreCacheControl>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.NamedMaximumNumberOfRegisters, OperandQuantifier.One) => Append(operand.ToEnum<NamedMaximumNumberOfRegisters>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FPEncoding, OperandQuantifier.One) => Append(operand.ToEnum<FPEncoding>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     (OperandKind.FPEncoding, OperandQuantifier.ZeroOrOne) => Append(operand.ToEnum<FPEncoding>().ToString(), ConsoleColor.Yellow).Append(' '),
                    //     _ => throw new Exception($"Unhandled operand kind {operand.Kind} with quantifier {operand.Quantifier}"),
                    // };
                }
                AppendLine("");
            }
        }



        public readonly override string ToString() => builder.ToString();
    }


    struct MemberIndex : IEquatable<MemberIndex>, IDisposable
    {
        public int Ref;
        public MemoryOwner<int> Accessors;
        public MemberIndex(int @ref, ReadOnlySpan<int> accessors)
        {
            Ref = @ref;
            if (accessors.IsEmpty)
                Accessors = MemoryOwner<int>.Empty;
            else
            {
                Accessors = MemoryOwner<int>.Allocate(accessors.Length);
                accessors.CopyTo(Accessors.Span);
            }
        }

        public static implicit operator MemberIndex(int @ref) => new(@ref, []);
        public static implicit operator MemberIndex(ReadOnlySpan<int> refAndAccessors) => new(refAndAccessors[0], refAndAccessors[1..]);

        public readonly bool Equals(MemberIndex other)
            => other.Ref == Ref && other.Accessors.Span.SequenceEqual(Accessors.Span);
        public override readonly bool Equals(object? obj)
            => obj is MemberIndex index && Equals(index);
        public override int GetHashCode()
            => HashCode.Combine(Ref, Accessors.Span.GetDjb2HashCode());
        public readonly void Dispose() => Accessors.Dispose();
    }


    struct DisData : IDisposable
    {
        static int MAX_OFFSET = 16;
        public Dictionary<MemberIndex, string> NameTable { get; }
        public NewSpirvBuffer Buffer { get; }
        public int IdOffset { get; private set; }
        public bool UseNames { get; private set; }
        public bool WriteToConsole { get; private set; }

        public DisData(NewSpirvBuffer buffer, bool useNames, bool writeToConsole)
        {
            Buffer = buffer;
            NameTable = [];
            UseNames = useNames;
            WriteToConsole = writeToConsole;
            ComputeIdOffset();
        }

        void ComputeIdOffset()
        {
            IdOffset = 9;
            if (!UseNames)
            {
                var bound = Buffer.Header.Bound;
                IdOffset = 3;
                while (bound > 0)
                {
                    bound /= 10;
                    IdOffset += 1;
                }
            }
            else
            {
                var maxName = 0;
                foreach (var i in Buffer)
                {
                    if (i.Op == Op.OpName)
                    {
                        var nameInst = (OpName)i;
                        maxName = maxName > nameInst.Name.Length ? maxName : nameInst.Name.Length;
                    }
                    else if (i.Op == Op.OpMemberName)
                    {
                        var memberInst = (OpMemberName)i;
                        maxName = maxName > memberInst.Name.Length ? maxName : memberInst.Name.Length;
                    }
                }
                IdOffset += maxName;
            }
            IdOffset = Math.Min(IdOffset, MAX_OFFSET);
        }
        public readonly NewSpirvBuffer.Enumerator GetEnumerator() => Buffer.GetEnumerator();

        public readonly void Dispose()
        {
            foreach (var key in NameTable.Keys)
                key.Dispose();
        }
    }




}