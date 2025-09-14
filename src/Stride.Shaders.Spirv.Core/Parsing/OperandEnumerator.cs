using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// An instruction operands enumerator, useful for parsing instructions
/// </summary>
public ref struct OperandEnumerator(Instruction instruction)
{
    static OperandKind[] Pairs { get; } = Enum.GetValues<OperandKind>().Where(x => x.ToString().StartsWith("Pair")).ToArray();
    Instruction instruction = instruction;
    readonly Span<int> Operands => instruction.Operands;
    readonly LogicalOperandArray logicalOperands = InstructionInfo.GetInfo(instruction);
    int wid = 0;
    int oid = -1;

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
                    while (!Operands[wid].HasEndString())
                        wid += 1;
                    wid += 1;
                }
                else if (Pairs.Contains(logOp.Kind ?? throw new Exception("kind is inexistent")))
                    wid += 2;
                else
                    wid += 1;
                oid += 1;

            }
            else if (logOp.Quantifier == OperandQuantifier.ZeroOrOne)
            {
                if (
                    Pairs.Contains(logOp.Kind ?? throw new Exception("kind is inexistent"))
                    && wid < Operands.Length - 1
                )
                {
                    wid += 2;
                }
                else if (
                    logOp.Kind == OperandKind.LiteralString
                    && wid < Operands.Length
                )
                {
                    while (!Operands[wid].HasEndString())
                        wid += 1;
                    wid += 1;
                }
                else if (wid < Operands.Length)
                    wid += 1;
                oid += 1;

            }
            else if (logOp.Quantifier == OperandQuantifier.ZeroOrMore)
            {
                if (logOp.Kind == OperandKind.LiteralString)
                    throw new NotImplementedException("params of strings is not yet implemented");
                else if (
                    Pairs.Contains(logOp.Kind ?? throw new Exception())
                    && wid < Operands.Length - 2
                )
                    wid += 2;
                else if (wid < Operands.Length - 1)
                    wid += 1;
                else
                    oid += 1;

            }
            return wid < Operands.Length;
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
                while (!Operands[wid + length].HasEndString())
                    length += 1;
                length += 1;
                var result = new SpvOperand(OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, length));

                return result;
            }
            else if (Pairs.Contains(logOp.Kind ?? throw new NotImplementedException("")))
            {
                var result = new SpvOperand(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2));
                return result;
            }
            else
                return new(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1));
        }
        else
        {
            if (Pairs.Contains(logOp.Kind ?? OperandKind.None))
                return new(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2));
            else
                return new(logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands[wid..]);
        }
    }

}