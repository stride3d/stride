using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Tools;

[Flags]
public enum DisassemblerFlags
{
    Id = 1,
    Name = 2,
    InstructionIndex = 4,
}

public static partial class Spv
{
    public static string Dis(NewSpirvBuffer buffer, DisassemblerFlags flags = DisassemblerFlags.Name, bool writeToConsole = false)
    {
        var writer = new DisWriter(buffer, flags, writeToConsole);
        writer.Disassemble();
        return writer.ToString();
    }

    public static string Dis(SpirvReader reader, DisassemblerFlags flags = DisassemblerFlags.Name, bool writeToConsole = false)
    {
        using var buffer = new NewSpirvBuffer(reader.Words);
        var writer = new DisWriter(buffer, flags, writeToConsole);
        writer.Disassemble();
        return writer.ToString();
    }

    struct DisWriter(NewSpirvBuffer buffer, DisassemblerFlags flags = DisassemblerFlags.Name, bool writeToConsole = true)
    {
        DisData data = new(buffer, flags, writeToConsole);
        readonly StringBuilder builder = new();

        readonly DisWriter AppendLine(string text, ConsoleColor? color = null)
        {
            if (writeToConsole)
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
            }
            builder.AppendLine(text);
            return this;
        }
        readonly DisWriter Append<T>(T text, ConsoleColor? color = null)
        {
            if (data.WriteToConsole)
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
            }
            builder.Append(text);
            return this;
        }

        readonly DisWriter AppendIdRef(int id, bool useNames = true)
        {
            if (data.UseNames && useNames && data.NameTable.TryGetValue(id, out var name))
            {
                Append($"%{name}", ConsoleColor.Green);
                if (data.UseIds)
                {
                    Append("[");
                    Append($"{id}", ConsoleColor.Green);
                    Append("]");
                }
                Append(" ");
                return this;
            }


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

        readonly DisWriter AppendEnums(OperandKind kind, SpvOperand operand)
        {
            foreach (ref var value in operand.Words)
                Append(value.ToEnumValueString(kind), ConsoleColor.Yellow).Append(' ');
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
                    if (data.UseIds)
                    {
                        Append("[");
                        Append($"{id}", ConsoleColor.Cyan);
                        Append("]");

                    }
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

        public readonly void Disassemble()
        {
            DisHeader();
            foreach (var instruction in data)
            {
                if (instruction.Op == Op.OpName)
                {
                    var nameInst = (OpName)instruction;
                    var name = nameInst.Name.Replace(".", "_");
                    // Try to find an available name (in case there is a duplicate)
                    int tryCount = 0;
                    while (!data.UsedNames.Add(name))
                    {
                        name = $"{nameInst.Name}_{++tryCount}";
                    }
                    data.NameTable[nameInst.Target] = name;
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
                        OperandKind k => (operand.Quantifier, operand.Words.Length) switch
                        {
                            (OperandQuantifier.One or OperandQuantifier.ZeroOrOne, 1) => Append(operand.Words[0].ToEnumValueString(k), ConsoleColor.Yellow).Append(' '),
                            (OperandQuantifier.ZeroOrMore, > 0) => AppendEnums(k, operand).Append(' '),
                            (OperandQuantifier.ZeroOrOne or OperandQuantifier.ZeroOrMore, 0) => Append(""),
                            _ => throw new NotImplementedException("Unsupported image operands quantifier " + operand.Quantifier + " with length " + operand.Words.Length)
                        }
                    };
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
        public HashSet<string> UsedNames { get; } = new();
        public NewSpirvBuffer Buffer { get; }
        public int IdOffset { get; private set; }
        public DisassemblerFlags Flags { get; private set; }
        public bool UseNames => (Flags & DisassemblerFlags.Name) != 0;
        public bool UseIds => (Flags & DisassemblerFlags.Id) != 0;
        public bool WriteToConsole { get; private set; }

        public DisData(NewSpirvBuffer buffer, DisassemblerFlags flags = DisassemblerFlags.Name, bool writeToConsole = false)
        {
            Buffer = buffer;
            NameTable = [];
            Flags = flags;
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
                        var nameInst = new OpName(i);
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