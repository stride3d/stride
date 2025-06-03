using System.Text;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core;
using static Spv.Specification;
using System.Runtime.CompilerServices;

namespace Stride.Shaders.Spirv.Tools;


internal record struct NameId(string Name);

public partial struct SpirvDis<TBuffer>
    where TBuffer : ISpirvBuffer

{
    public readonly static int MAX_OFFSET = 16;
    TBuffer buffer;
    DisWriter writer = new();
    int IdOffset { get; init; }
    bool UseNames { get; init; }

    SortedList<int, NameId> nameTable = [];

    public SpirvDis(TBuffer buff, bool useNames = false)
    {
        buffer = buff;
        writer = new();
        UseNames = useNames;
        IdOffset = 9;
        if (!useNames)
        {
            var bound = buff.Header.Bound;
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
            foreach (var i in buffer)
            {
                if (
                    (i.OpCode == SDSLOp.OpName || i.OpCode == SDSLOp.OpMemberName)
                    && i.TryGetOperand("name", out LiteralString? name)
                    && name is not null
                )
                {
                    maxName = maxName > name.Value.Value.Length ? maxName : name.Value.Value.Length;
                }
            }
            IdOffset += maxName;
        }
        IdOffset = Math.Min(IdOffset, MAX_OFFSET);
    }


    public string Disassemble(bool writeToConsole = false)
    {
        writer = writer with { WriteToConsole = writeToConsole };
        writer.Clear();

        if (buffer.HasHeader)
        {
            var header = buffer.Header;
            writer
                .AppendLine("; SPIR-V")
                .AppendLine($"; Version: {header.Version}")
                .AppendLine($"; Generator: {header.GeneratorMagicNumber}")
                .AppendLine($"; Bound: {header.Bound}")
                .AppendLine($"; Schema: {header.Schema}");
        }

        foreach (var e in buffer)
        {
            CheckNameTable(e);

            if (UseNames && e.ResultId is int id && nameTable.TryGetValue(id, out var nid))
                Append(nid);
            else
                Append(e.ResultId != null ? new IdResult(e.ResultId.Value) : null);

            writer.Append(' ');
            if (e.ResultId is int)
                writer.Append('=');

            AppendLiteral(Enum.GetName(e.OpCode) ?? "Op.OpNop");
            foreach (var o in e)
                Append(o, e);

            AppendLine();
        }
        return writer.ToString();
    }

    // TODO :  add other names
    public readonly void CheckNameTable(RefInstruction instruction)
    {
        if (
            UseNames
            && (instruction.OpCode == SDSLOp.OpName || instruction.OpCode == SDSLOp.OpMemberName)
            && instruction.TryGetOperand("target", out IdRef? target) && target is IdRef t
            && instruction.TryGetOperand("name", out LiteralString? name) && name is LiteralString n
            )
        {
            nameTable[t] = new(n.Value);
        }
        else if (instruction.OpCode == SDSLOp.OpTypeVoid)
            nameTable[instruction.ResultId!.Value] = new("void");
        else if (instruction.OpCode == SDSLOp.OpTypeBool)
            nameTable[instruction.ResultId!.Value] = new("bool");
        else if (instruction.OpCode == SDSLOp.OpTypeInt)
        {
            var type = instruction.Operands[1..] switch
            {
                [8, 0] => "byte",
                [16, 0] => "ushort",
                [32, 0] => "uint",
                [64, 0] => "ulong",
                [8, 1] => "sbyte",
                [16, 1] => "short",
                [32, 1] => "int",
                [64, 1] => "long",
                _ => "int"
            };
            nameTable[instruction.ResultId!.Value] = new(type);
        }
        else if (instruction.OpCode == SDSLOp.OpTypeFloat)
        {
            var size = instruction.Operands[1];
            nameTable[instruction.ResultId!.Value] = new(size == 16 ? "half" : size == 64 ? "double" : "float");
        }
        else if (instruction.OpCode == SDSLOp.OpTypeVector)
        {
            nameTable[instruction.ResultId!.Value] = new(nameTable[instruction.Operands[1]].Name + instruction.Operands[2]);
        }


    }


    public readonly override string ToString()
    {
        return writer.ToString();
    }
}