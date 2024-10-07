using Stride.Shaders.Spirv.Core.Parsing;
using System.Text;
using System.Transactions;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;


/// <summary>
/// A spirv buffer composed of many different buffers for declarations and functions
/// </summary>
public sealed partial class MultiBuffer : IMutSpirvBuffer
{
    public int Bound { get; private set; }
    public int Length => Declarations.Length + Functions.BuffersLength;

    public WordBuffer Declarations { get; init; }
    public FunctionBufferCollection Functions { get; init; }

    public MultiBufferInstructions Instructions => new(this);

    public MultiBuffer()
    {
        Declarations = new();
        Functions = new();
    }

    public Instruction Add(MutRefInstruction instruction)
    {
        if (instruction.OpCode == SDSLOp.OpSDSLFunction)
        {
            var name = instruction.GetOperand<LiteralString>("functionName");
            var id = instruction.GetOperand<IdResult>("resultId").Value;
            Declarations.AddOpName(id, name);
            Span<int> words = stackalloc int[5];
            instruction.Words[..5].CopyTo(words);
            var funcInstruction = new MutRefInstruction(words);
            funcInstruction.OpCode = SDSLOp.OpFunction;
            return Functions.Insert(funcInstruction, name.Value);
        }
        else if(instruction.OpCode == SDSLOp.OpFunction)
        {
            var n = "";
            foreach(var i in Declarations)
            {
                if (i.OpCode == SDSLOp.OpName && i.Words.Span[1] == instruction.Words[2])
                    n = i.GetOperand<LiteralString>("name")?.Value ?? "";
            }
            return Functions.Insert(instruction, n);
        }
        else
        {
            return InstructionInfo.GetGroupOrder(instruction) switch
            {
                13 => Functions.Insert(instruction),
                _ => Declarations.Add(instruction)
            };
        }
    }
    public Instruction Duplicate(RefInstruction instruction, int offset = 0)
    {
        var m = new MutRefInstruction(stackalloc int[instruction.WordCount]);
        m.OpCode = instruction.OpCode;
        m.WordCount = instruction.WordCount;
        instruction.Operands.CopyTo(m.Words[1..]);
        if (offset > 0)
        {
            foreach (var op in m)
            {
                if (
                    op.Kind == OperandKind.IdResult
                    || op.Kind == OperandKind.IdResultType
                    || op.Kind == OperandKind.IdRef
                    || op.Kind == OperandKind.PairIdRefIdRef
                    || op.Kind == OperandKind.PairIdRefLiteralInteger
                    )
                    op.Words[0] += offset;
                if (op.Kind == OperandKind.PairIdRefIdRef || op.Kind == OperandKind.PairLiteralIntegerIdRef)
                {
                    op.Words[1] += offset;
                }
            }
        }
        return Add(m);
    }

    public int GetNextId()
    {
        Bound += 1;
        return Bound;
    }

    internal static int GetWordLength<T>(T? value)
    {
        if (value is null) return 0;

        return value switch
        {
            LiteralInteger i => i.WordCount,
            LiteralFloat i => i.WordCount,
            int _ => 1,
            IdRef _ => 1,
            IdResultType _ => 1,
            IdResult _ => 1,
            string v => new LiteralString(v).WordCount,
            LiteralString v => v.WordCount,
            int[] a => a.Length,
            Enum _ => 1,
            _ => throw new NotImplementedException()
        };
    }


    public void Dispose()
    {
        Declarations.Dispose();
        foreach (var function in Functions)
            function.Value.Dispose();
    }

    public struct MultiBufferInstructions
    {
        MultiBuffer buffers;
        public MultiBufferInstructions(MultiBuffer buffers)
        {
            this.buffers = buffers;
        }

        public Enumerator GetEnumerator() => new(buffers);
        
        public ref struct Enumerator
        {
            MultiBuffer buffers;
            OrderedEnumerator declarationEnumerator;
            FunctionBufferCollection.FunctionsInstructions.Enumerator functionsEnumerator;
            bool started;
            bool declarationsFinished;
            public Enumerator(MultiBuffer buffers)
            {
                this.buffers = buffers;
                declarationEnumerator = buffers.Declarations.GetEnumerator();
                functionsEnumerator = buffers.Functions.Instructions.GetEnumerator();
                started = false;
                declarationsFinished = false;
            }

            public Instruction Current => !declarationsFinished ? declarationEnumerator.Current : functionsEnumerator.Current;

            public bool MoveNext()
            {
                if(!started)
                {
                    started = true;
                    if (declarationEnumerator.MoveNext())
                        return true;
                    else
                        declarationsFinished = true;
                    if (functionsEnumerator.MoveNext())
                        return true;
                    return false;
                }
                else
                {
                    if(!declarationsFinished)
                    {
                        if (declarationEnumerator.MoveNext())
                            return true;
                        else
                            declarationsFinished = true;
                    }
                    return functionsEnumerator.MoveNext();
                }
            }
        }
    }

    public void RecomputeBound()
    {
        var b = 0;
        foreach(var i in Declarations.UnorderedInstructions)
        {
            var id = i.ResultId;
            if (id != null && id > b)
                b = id ?? -1;
        }
        foreach(var (_,f) in Functions)
        foreach (var i in f.UnorderedInstructions)
        {
            var id = i.ResultId;
            if (id != null && id > b)
                b = id ?? -1;
        }
        Bound = b + 1;
    }
    public override string ToString()
    {
        return
            new StringBuilder()
            .Append(Disassembler.Disassemble(Declarations))
            .Append(string.Join("\n", Functions.Buffers.Select(x => Disassembler.Disassemble(x.Value))))
            .ToString();
    }
    
}

// public static class MBExtensions
// {
//     public static Instruction AddOpDecorate(this MultiBuffer mb, IdRef target, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)
//     {
//         var wordLength = 1 + MultiBuffer.GetWordLength(target) + MultiBuffer.GetWordLength(decoration) + MultiBuffer.GetWordLength(additional1) + MultiBuffer.GetWordLength(additional2) + MultiBuffer.GetWordLength(additionalString);
//         var mri = new MutRefInstruction([wordLength << 16 | (int)SDSLOp.OpDecorate, target, (int)decoration, ..additional1.ToSpirvSpanOwner().Span, ..additional2.ToSpirvSpanOwner().Span, ..additionalString.ToSpirvSpanOwner().Span]);
//         return mb.Add(mri);
//     }
// }