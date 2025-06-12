using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// An instruction operands enumerator, useful for parsing instructions
/// </summary>
public ref struct OperandEnumerator
{
    static OperandKind[] pairs { get; } = Enum.GetValues<OperandKind>().Where(x => x.ToString().StartsWith("Pair")).ToArray();
    RefInstruction instruction;
    Span<int> operands => instruction.Operands;
    readonly LogicalOperandArray logicalOperands;
    int wid;
    int oid;

    public OperandEnumerator(RefInstruction instruction)
    {
        this.instruction = instruction;
        Decoration? decoration = instruction.OpCode switch
        {
            SDSLOp.OpDecorateString
            or SDSLOp.OpDecorate
            or SDSLOp.OpDecorateId => (Decoration)instruction.Operands[1],
            SDSLOp.OpMemberDecorate
            or SDSLOp.OpMemberDecorateString => (Decoration)instruction.Operands[2],
            _ => null
        };
        logicalOperands = InstructionInfo.GetInfo(new(instruction.OpCode, decoration));
        oid = -1;
        wid = 0;
    }

    public SpvOperand Current => ParseCurrent();

    public bool MoveNext()
    {
        if (oid < 0)
        {
            oid = 0;
            if (logicalOperands[0].Kind == OperandKind.None)
                return false;
            return true;
        }
        else if(oid >= logicalOperands.Count - 1)
            return false;
        else
        {
            var logOp = logicalOperands[oid];

            if (logOp.Quantifier == OperandQuantifier.One)
            {
                if (logOp.Kind == OperandKind.LiteralString)
                {
                    while (!operands[wid].HasEndString())
                        wid += 1;
                    wid += 1;
                }
                else if (pairs.Contains(logOp.Kind ?? throw new Exception("kind is inexistent")))
                    wid += 2;
                else
                    wid += 1;
                oid += 1;

            }
            else if (logOp.Quantifier == OperandQuantifier.ZeroOrOne)
            {
                if (
                    pairs.Contains(logOp.Kind ?? throw new Exception("kind is inexistent"))
                    && wid < operands.Length - 1
                )
                {
                    wid += 2;
                }
                else if (
                    logOp.Kind == OperandKind.LiteralString
                    && wid < operands.Length
                )
                {
                    while (!operands[wid].HasEndString())
                        wid += 1;
                    wid += 1;
                }
                else if (wid < operands.Length)
                    wid += 1;
                oid += 1;

            }
            else if (logOp.Quantifier == OperandQuantifier.ZeroOrMore)
            {
                if (logOp.Kind == OperandKind.LiteralString)
                    throw new NotImplementedException("params of strings is not yet implemented");
                else if (
                    pairs.Contains(logOp.Kind ?? throw new Exception())
                    && wid < operands.Length - 2
                )
                    wid += 2;
                else if (wid < operands.Length - 1)
                    wid += 1;
                else
                    oid += 1;

            }
            return wid < operands.Length;
        }

    }

    public SpvOperand ParseCurrent()
    {
        var logOp = logicalOperands[oid];
        // if (instruction.OpCode == SDSLOp.OpDecorate)
        // {
        //     SpvOperand result = new();
        //     if (oid == 0)
        //         result = new(OperandKind.IdRef, OperandQuantifier.One, operands.Slice(wid, 1));
        //     else if (oid == 1)
        //         result = new(OperandKind.Decoration, OperandQuantifier.One, operands.Slice(wid, 1));
        //     else if (oid == 2)
        //     {
        //         result = result with
        //         {
        //             Kind = (Decoration)operands[1] switch
        //             {
        //                 Decoration.BuiltIn => OperandKind.BuiltIn,
        //                 Decoration.Location => OperandKind.LiteralInteger,
        //                 Decoration.SpecId => OperandKind.LiteralSpecConstantOpInteger,
        //                 Decoration.ArrayStride => OperandKind.LiteralInteger,
        //                 Decoration.MatrixStride => OperandKind.LiteralInteger,
        //                 Decoration.UniformId => OperandKind.IdScope,
        //                 Decoration.Stream => OperandKind.LiteralInteger,
        //                 Decoration.Component => OperandKind.LiteralInteger,
        //                 Decoration.Index => OperandKind.LiteralInteger,
        //                 Decoration.Binding => OperandKind.LiteralInteger,
        //                 Decoration.DescriptorSet => OperandKind.LiteralInteger,
        //                 Decoration.Offset => OperandKind.LiteralInteger,
        //                 Decoration.XfbBuffer => OperandKind.LiteralInteger,
        //                 Decoration.XfbStride => OperandKind.LiteralInteger,
        //                 Decoration.FuncParamAttr => OperandKind.FunctionParameterAttribute,
        //                 Decoration.FPRoundingMode => OperandKind.FPRoundingMode,
        //                 Decoration.FPFastMathMode => OperandKind.FPFastMathMode,
        //                 Decoration.LinkageAttributes => OperandKind.LiteralString,
        //                 Decoration.InputAttachmentIndex => OperandKind.LiteralInteger,
        //                 Decoration.Alignment => OperandKind.LiteralInteger,
        //                 Decoration.MaxByteOffset => OperandKind.LiteralInteger,
        //                 Decoration.AlignmentId => OperandKind.IdRef,
        //                 Decoration.MaxByteOffsetId => OperandKind.IdRef,
        //                 Decoration.SecondaryViewportRelativeNV => OperandKind.LiteralInteger,
        //                 Decoration.CounterBuffer => OperandKind.IdRef,
        //                 _ => OperandKind.None
        //             }
        //         };
        //     }
        //     return result;

        // }
        // else
        if (logOp.Quantifier != OperandQuantifier.ZeroOrMore)
        {
            if (logOp.Kind == OperandKind.LiteralString)
            {
                var length = 0;
                while (!operands[wid + length].HasEndString())
                    length += 1;
                length += 1;
                var result = new SpvOperand(OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, operands.Slice(wid, length));

                return result;
            }
            else if (pairs.Contains(logOp.Kind ?? throw new NotImplementedException("")))
            {
                var result = new SpvOperand(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, operands.Slice(wid, 2));
                return result;
            }
            else
                return new(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, operands.Slice(wid, 1));
        }
        else
        {
            if (pairs.Contains(logOp.Kind ?? OperandKind.None))
                return new(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, operands.Slice(wid, 2));
            else
                return new(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, operands.Slice(wid, 1));
        }
    }

}

public static class IntExtensions
{
    public static bool HasEndString(this int i)
    {
        return
            (char)(i >> 24) == '\0'
            || (char)(i >> 16 & 0XFF) == '\0'
            || (char)(i >> 8 & 0xFF) == '\0'
            || (char)(i & 0xFF) == '\0';
    }
}