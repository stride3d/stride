using static Spv.Specification;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv.Tools;


public partial struct SpirvDis<TBuffer>
{
    public readonly void Append(IdResult? result)
    {

        if (result != null)
        {
            var tmp = result.Value;
            var size = 1;
            while (tmp > 0)
            {
                tmp /= 10;
                size += 1;
            }
            writer.Append(' ', IdOffset - 1 - size).Append('%', ConsoleColor.Blue).Append(result!.Value.Value, ConsoleColor.Blue);
        }
        else
            writer.Append(' ', IdOffset);

    }
    internal readonly void Append(NameId name)
    {
        writer.Append(' ', Math.Max(0, IdOffset - 2 - name.Name.Length)).Append('%', ConsoleColor.Blue).Append(name.Name, ConsoleColor.Blue);
    }

    public readonly void Append<T>(T value) where T : Enum
    {
        var name = Enum.GetName(typeof(T), value);
        writer.Append(' ').Append(name);
    }
    public readonly void Append(IdRef id, bool ignoreName = false)
    {

        if (UseNames && !ignoreName && nameTable.TryGetValue(id, out var name))
            writer.Append(" %", ConsoleColor.DarkYellow).Append(name.Name, ConsoleColor.DarkYellow);
        else
            writer.Append(" %", ConsoleColor.DarkYellow).Append(id.Value, ConsoleColor.DarkYellow);
    }
    public readonly void Append(IdResultType id)
    {
        if (UseNames && nameTable.TryGetValue(id, out var name))
            writer.Append(" %", ConsoleColor.DarkYellow).Append(name.Name, ConsoleColor.DarkYellow);
        else
            writer.Append(" %", ConsoleColor.DarkYellow).Append(id.Value, ConsoleColor.DarkYellow);
    }
    public readonly void AppendInt(int v)
    {
        writer.Append(' ').Append(v, ConsoleColor.Red);
    }
    public readonly void AppendConst(int typeId, Span<int> words)
    {
        writer.Append(' ');
        foreach (var e in buffer)
        {
            if (e.ResultId is int rid && rid == typeId)
            {
                if (e.OpCode == SDSLOp.OpTypeInt)
                {
                    writer.Append(words.Length == 1 ? words[0] : words[0] << 32 | words[1], ConsoleColor.Red);
                    return;
                }
                else if (e.OpCode == SDSLOp.OpTypeFloat)
                {
                    writer.Append(
                        words.Length == 1 ?
                            BitConverter.Int32BitsToSingle(words[0])
                            : BitConverter.Int64BitsToDouble(words[0] << 32 | words[1]),

                        ConsoleColor.Red
                    );
                    return;
                }
            }
        }
    }
    public readonly void AppendLiteral(LiteralInteger v)
    {
        writer.Append(' ').Append(v.Words, ConsoleColor.Red);
    }

    public readonly void AppendLiteral(LiteralFloat v)
    {
        if (v.WordCount == 1)
            writer.Append(' ').Append(Convert.ToSingle(v.Words & 0xFFFF), ConsoleColor.Red);
        else if (v.WordCount == 2)
            writer.Append(' ').Append(Convert.ToDouble(v.Words), ConsoleColor.Red);
    }
    public readonly void AppendLiteral(LiteralString v, bool quoted = false)
    {
        if (!quoted)
            writer.Append(' ').Append(v.Value);
        else
            writer.Append(' ').Append('"').Append(v.Value, ConsoleColor.Green).Append('"');
    }
    public readonly void Append(PairLiteralIntegerIdRef v)
    {
        (int, int) value = v;
        AppendInt(value.Item1);
        Append(new IdRef(value.Item2));
    }
    public readonly void Append(PairIdRefLiteralInteger v)
    {
        (int, int) value = v;
        Append(new IdRef(value.Item1));
        AppendInt(value.Item2);
    }
    public readonly void Append(PairIdRefIdRef v)
    {
        (int, int) value = v;
        Append(new IdRef(value.Item1));
        Append(new IdRef(value.Item2));
    }
    public readonly void AppendLine() => writer.AppendLine();

    public readonly void Append(in SpvOperand o, in RefInstruction instruction)
    {
        if (o.Kind == OperandKind.IdRef)
            foreach (var e in o.Words)
                Append(new IdRef(e), false);
        else if (o.Kind == OperandKind.IdResultType)
            foreach (var e in o.Words)
                Append((IdResultType)e);
        else if (o.Kind == OperandKind.PairLiteralIntegerIdRef)
            for (int i = 0; i < o.Words.Length; i += 2)
                Append(new PairLiteralIntegerIdRef((o.Words[i], o.Words[i + 1])));
        else if (o.Kind == OperandKind.PairIdRefLiteralInteger)
            for (int i = 0; i < o.Words.Length; i += 2)
                Append(new PairIdRefLiteralInteger((o.Words[i], o.Words[i + 1])));
        else if (o.Kind == OperandKind.PairIdRefIdRef)
            for (int i = 0; i < o.Words.Length; i += 2)
                Append(new PairIdRefIdRef((o.Words[i], o.Words[i + 1])));
        else if (
                o.Kind == OperandKind.LiteralContextDependentNumber
                && (instruction.OpCode == SDSLOp.OpConstant || instruction.OpCode == SDSLOp.OpSpecConstant)
                && instruction.ResultType is int rtype
            )
        {
            AppendConst(rtype, o.Words);
        }
        else if (o.Kind == OperandKind.LiteralContextDependentNumber)
            AppendLiteral(o.To<LiteralInteger>());
        else if (o.Kind == OperandKind.PackedVectorFormat)
            foreach (var e in o.Words)
                Append((PackedVectorFormat)e);
        else if (o.Kind == OperandKind.ImageOperands)
            foreach (var e in o.Words)
                Append((ImageOperandsMask)e);
        else if (o.Kind == OperandKind.FPFastMathMode)
            foreach (var e in o.Words)
                Append((FPFastMathModeMask)e);
        else if (o.Kind == OperandKind.SelectionControl)
            foreach (var e in o.Words)
                Append((SelectionControlMask)e);
        else if (o.Kind == OperandKind.LoopControl)
            foreach (var e in o.Words)
                Append((LoopControlMask)e);
        else if (o.Kind == OperandKind.FunctionControl)
            foreach (var e in o.Words)
                Append((FunctionControlMask)e);
        else if (o.Kind == OperandKind.MemorySemantics)
            foreach (var e in o.Words)
                Append((MemorySemanticsMask)e);
        else if (o.Kind == OperandKind.MemoryAccess)
            foreach (var e in o.Words)
                Append((MemoryAccessMask)e);
        else if (o.Kind == OperandKind.KernelProfilingInfo)
            foreach (var e in o.Words)
                Append((KernelProfilingInfoMask)e);
        else if (o.Kind == OperandKind.RayFlags)
            foreach (var e in o.Words)
                Append((RayFlagsMask)e);
        else if (o.Kind == OperandKind.FragmentShadingRate)
            foreach (var e in o.Words)
                Append((FragmentShadingRateMask)e);
        else if (o.Kind == OperandKind.SourceLanguage)
            foreach (var e in o.Words)
                Append((SourceLanguage)e);
        else if (o.Kind == OperandKind.ExecutionModel)
            foreach (var e in o.Words)
                Append((ExecutionModel)e);
        else if (o.Kind == OperandKind.AddressingModel)
            foreach (var e in o.Words)
                Append((AddressingModel)e);
        else if (o.Kind == OperandKind.MemoryModel)
            foreach (var e in o.Words)
                Append((MemoryModel)e);
        else if (o.Kind == OperandKind.ExecutionMode)
            foreach (var e in o.Words)
                Append((ExecutionMode)e);
        else if (o.Kind == OperandKind.StorageClass)
            foreach (var e in o.Words)
                Append((StorageClass)e);
        else if (o.Kind == OperandKind.Dim)
            foreach (var e in o.Words)
                Append((Dim)e);
        else if (o.Kind == OperandKind.SamplerAddressingMode)
            foreach (var e in o.Words)
                Append((SamplerAddressingMode)e);
        else if (o.Kind == OperandKind.SamplerFilterMode)
            foreach (var e in o.Words)
                Append((SamplerFilterMode)e);
        else if (o.Kind == OperandKind.ImageFormat)
            foreach (var e in o.Words)
                Append((ImageFormat)e);
        else if (o.Kind == OperandKind.ImageChannelOrder)
            foreach (var e in o.Words)
                Append((ImageChannelOrder)e);
        else if (o.Kind == OperandKind.ImageChannelDataType)
            foreach (var e in o.Words)
                Append((ImageChannelDataType)e);
        else if (o.Kind == OperandKind.FPRoundingMode)
            foreach (var e in o.Words)
                Append((FPRoundingMode)e);
        else if (o.Kind == OperandKind.LinkageType)
            foreach (var e in o.Words)
                Append((LinkageType)e);
        else if (o.Kind == OperandKind.AccessQualifier)
            foreach (var e in o.Words)
                Append((AccessQualifier)e);
        else if (o.Kind == OperandKind.FunctionParameterAttribute)
            foreach (var e in o.Words)
                Append((FunctionParameterAttribute)e);
        else if (o.Kind == OperandKind.Decoration)
            foreach (var e in o.Words)
                Append((Decoration)e);
        else if (o.Kind == OperandKind.BuiltIn)
            foreach (var e in o.Words)
                Append((BuiltIn)e);
        else if (o.Kind == OperandKind.Scope)
            foreach (var e in o.Words)
                Append((Scope)e);
        else if (o.Kind == OperandKind.GroupOperation)
            foreach (var e in o.Words)
                Append((GroupOperation)e);
        else if (o.Kind == OperandKind.KernelEnqueueFlags)
            foreach (var e in o.Words)
                Append((KernelEnqueueFlags)e);
        else if (o.Kind == OperandKind.Capability)
            foreach (var e in o.Words)
                Append((Capability)e);
        else if (o.Kind == OperandKind.RayQueryIntersection)
            foreach (var e in o.Words)
                Append((RayQueryIntersection)e);
        else if (o.Kind == OperandKind.RayQueryCommittedIntersectionType)
            foreach (var e in o.Words)
                Append((RayQueryCommittedIntersectionType)e);
        else if (o.Kind == OperandKind.RayQueryCandidateIntersectionType)
            foreach (var e in o.Words)
                Append((RayQueryCandidateIntersectionType)e);
        else if (o.Kind == OperandKind.IdMemorySemantics)
            foreach (var e in o.Words)
                AppendInt((IdMemorySemantics)e);
        else if (o.Kind == OperandKind.IdScope)
            foreach (var e in o.Words)
                AppendInt((IdScope)e);
        else if (o.Kind == OperandKind.IdRef)
            foreach (var e in o.Words)
                Append((IdRef)e);
        else if (o.Kind == OperandKind.LiteralInteger)
            foreach (var e in o.Words)
                AppendInt(e);
        else if (o.Kind == OperandKind.LiteralString)
            AppendLiteral(new LiteralString(o.Words), quoted: true);

    }
}