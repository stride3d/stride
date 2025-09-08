using System.Numerics;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Tools;

public static partial class Spv
{
    public static string Dis(NewSpirvBuffer buffer, bool useNames = true, bool writeToConsole = true)
    {
        // this.buffer = buffer;
        // ComputeIdOffset();
        // Assembly code generation logic goes here
        var writer = new DisWriter(buffer, useNames, writeToConsole);
        writer.Disassemble();
        foreach (var instruction in data)
        {
            // Disassemble each instruction
        }
        return "";
    }



    struct DisWriter(NewSpirvBuffer buffer, bool useNames = true, bool writeToConsole = true)
    {
        DisData data = new(buffer, useNames, writeToConsole);
        readonly StringBuilder builder = new();

        DisWriter AppendLine(string text, ConsoleColor? color = null)
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
        DisWriter Append<T>(T text, ConsoleColor? color = null)
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
        DisWriter AppendRepeatChar(char c, int count)
        {
            if (data.WriteToConsole)
                while (count-- > 0)
                    Console.Write(c);
            builder.Append(c, count);
            return this;
        }
        DisWriter AppendLiteralNumber<T>(T value)
            where T : struct, INumber<T>
        {
            Append(value, ConsoleColor.Red).Append(' ');
            return this;
        }

        DisWriter AppendLiteralNumber<T>(LiteralValue<T> value, bool dispose = true)
            where T : struct, INumber<T>
        {
            Append(value.Value, ConsoleColor.Red).Append(' ');
            if (dispose)
                value.Dispose();
            return this;
        }

        DisWriter AppendLiteralString(LiteralValue<string> value, bool dispose = true)
        {
            Append('"', ConsoleColor.Green).Append(value.Value, ConsoleColor.Green).Append('"', ConsoleColor.Green).Append(' ');
            if (dispose)
                value.Dispose();
            return this;
        }
        DisWriter AppendLiteralString(string value)
        {
            Append('"', ConsoleColor.Green).Append(value, ConsoleColor.Green).Append('"', ConsoleColor.Green).Append(' ');
            return this;
        }

        DisWriter AppendLiteralNumbers<T>(LiteralArray<T> value, bool dispose = true)
            where T : struct, INumber<T>
        {
            T tmp = default;
            var size = tmp switch
            {
                byte or sbyte or short or ushort or int or uint or float => 1,
                long or ulong or double => 2,
                _ => throw new NotImplementedException("Cannot create LiteralValue from the provided words")
            };
            for(int i = 0; i < value.WordCount; i += size)
            {
                if (size == 1)
                {
                    using var lit = LiteralValue<T>.From([value.Words[i]]);
                    Append(lit.Value, ConsoleColor.Red);
                }
                else
                {
                    using var v = LiteralValue<T>.From([(value.Words[i] << 32 | value.Words[i + 1])]);
                    Append(v.Value, ConsoleColor.Red);
                }
            }
            if (dispose)
                value.Dispose();
            return this;
        }
        DisWriter AppendResultId(int? id = null)
        {
            if (id is int i)
            {
                if (data.UseNames && data.NameTable.TryGetValue(i, out var name))
                {
                    AppendRepeatChar(' ', data.IdOffset - name.Length - 1);
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
                AppendRepeatChar(' ', data.IdOffset - 1);
            return this;

        }

        public void Disassemble()
        {
            DisHeader();
            foreach (var instruction in data)
            {
                DisInstruction(instruction, this);
            }
        }

        public void DisHeader()
        {
            var header = data.Buffer.Header;
            AppendLine($"; SPIR-V");
            AppendLine($"; Version: {header.Version}");
            AppendLine($"; Generator: {header.Generator}");
            AppendLine($"; Bound: {header.Bound}");
            AppendLine($"; Schema: {header.Schema}");
            AppendLine("");
        }

        public void DisInstruction(in OpDataIndex instruction, in DisWriter writer)
        {
            if (instruction.Op == Op.OpName)
            {
                var nameInst = (OpName)instruction;
                data.NameTable[nameInst.Target] = nameInst.Name;
                AppendResultId();
                Append("OpName ").AppendLiteralNumber(nameInst.Target).AppendLiteralString(nameInst.Name);
            }
            else if (instruction.Op == Op.OpMemberName)
            {
                var memberInst = (OpMemberName)instruction;
                data.NameTable[memberInst.Type + memberInst.Member] = memberInst.Name;
                AppendResultId();
                Append("OpMemberName ").AppendLiteralNumber(memberInst.Type).AppendLiteralNumber(memberInst.Member).AppendLiteralString(memberInst.Name);
            }
            else
            {
                ref var data = ref instruction.Data;
                var info = InstructionInfo.GetInfo(data);
                if (info.GetResultIndex(out int resultIndex))
                    AppendResultId(data.Memory.Span[resultIndex]);
                else
                    AppendResultId();
                Append(instruction.Op.ToString()).Append(' ');
                JsonElement e;
                e.TryGetProperty()
                foreach (var operand in data)
                {
                    _ = operand.Kind switch
                    {
                        OperandKind.LiteralString => AppendLiteralString(operand.To<LiteralValue<string>>().Value),
                        _ => this
                    };
                }
            }
        }

        public override string ToString() => builder.ToString();
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
            this.UseNames = useNames;
            this.WriteToConsole = writeToConsole;
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
                    maxName = i.Op switch
                    {
                        Op.OpName => maxName > ((OpName)i).Name.Length ? maxName : ((OpName)i).Name.Length,
                        Op.OpMemberName => maxName > ((OpMemberName)i).Name.Length ? maxName : ((OpMemberName)i).Name.Length,
                        _ => maxName
                    };
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