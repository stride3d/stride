using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// An instruction operands enumerator, useful for parsing instructions
/// </summary>
public ref struct OpDataEnumerator
{
    readonly Span<int> instruction;
    readonly Span<int> Operands => instruction[1..];
    readonly Op OpCode => (Op)(instruction[0] & 0xFFFF);
    readonly LogicalOperandArray logicalOperands;
    int wid;
    int oid;

    public OpDataEnumerator(Span<int> instruction)
    {
        this.instruction = instruction;

        Decoration? decoration = null;
        switch (OpCode)
        {
            case Op.OpDecorate:
            case Op.OpDecorateId:
            case Op.OpDecorateString:
                decoration = (Decoration)instruction[2];
                break;
            case Op.OpMemberDecorate:
            case Op.OpMemberDecorateString:
                decoration = (Decoration)instruction[3];
                break;
        }

        logicalOperands = InstructionInfo.GetInfo(new OperandKey(OpCode, decoration));
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
        else
        {
            var logOp = logicalOperands[oid];
            (bool result, int newWid, int newOid) = OpCode switch
            {
                Op.OpDecorate => oid switch
                {
                    0 => (true, wid + 1, oid + 1),
                    _ => (Decoration)Operands[wid] switch
                    {
                        Decoration.BuiltIn
                        or Decoration.Location
                        or Decoration.SpecId
                        or Decoration.ArrayStride
                        or Decoration.MatrixStride
                        or Decoration.UniformId
                        or Decoration.Stream
                        or Decoration.Component
                        or Decoration.Index
                        or Decoration.Binding
                        or Decoration.DescriptorSet
                        or Decoration.Offset
                        or Decoration.XfbBuffer
                        or Decoration.XfbStride
                        or Decoration.FuncParamAttr
                        or Decoration.FPRoundingMode
                        or Decoration.FPFastMathMode
                        or Decoration.InputAttachmentIndex
                        or Decoration.Alignment
                        or Decoration.MaxByteOffset
                        or Decoration.AlignmentId
                        or Decoration.MaxByteOffsetId
                        or Decoration.SecondaryViewportRelativeNV
                        or Decoration.CounterBuffer => (true, wid + 1, oid + 1),
                        Decoration.LinkageAttributes => throw new NotImplementedException(),
                        _ => (false, wid, oid)
                    }
                },
                _ => logOp switch
                {
                    { Quantifier: OperandQuantifier.One, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                        => (true, wid + 2, oid + 1),
                    { Quantifier: OperandQuantifier.One, Kind: OperandKind.LiteralString } => (true, wid + Operands[wid..].LengthOfString(), oid + 1),
                    { Quantifier: OperandQuantifier.One, Kind: _ } => (true, wid + 1, oid + 1),
                    { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                        => (wid < Operands.Length - 2, wid + (wid < Operands.Length - 2 ? 2 : 0), oid + wid < Operands.Length - 2 ? 1 : 0),
                    { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.LiteralString }
                        => (wid < Operands.Length - 1, wid + (wid < Operands.Length - 1 ? Operands[wid..].LengthOfString() : 0), oid + wid < Operands.Length ? 1 : 0),
                    { Quantifier: OperandQuantifier.ZeroOrOne, Kind: _ }
                        => (wid < Operands.Length - 1, wid + (wid < Operands.Length ? 1 : 0), oid + wid < Operands.Length ? 1 : 0),
                    { Quantifier: OperandQuantifier.ZeroOrMore }
                        => (wid < Operands.Length - 1, wid < Operands.Length - 1 ? Operands.Length : wid, oid + (wid < Operands.Length - 1 ? 0 : 1)),
                    _ => (false, wid, oid)
                }
            };
            wid = newWid;
            oid = newOid;
            if (oid < logicalOperands.Count)
                return logicalOperands[oid].Quantifier switch
                {
                    OperandQuantifier.One => result && wid < Operands.Length && oid < logicalOperands.Count,
                    _ => result && oid < logicalOperands.Count
                };
            else return false;
        }
    }

    public SpvOperand ParseCurrent()
    {
        var logOp = logicalOperands[oid];

        return OpCode switch
        {
            Op.OpDecorate => oid switch
            {
                0 => new(OperandKind.IdRef, OperandQuantifier.One, Operands.Slice(wid, 1)),
                1 => new(OperandKind.Decoration, OperandQuantifier.One, Operands.Slice(wid, 1)),
                2 => new SpvOperand() with
                {
                    Kind = (Decoration)Operands[1] switch
                    {
                        Decoration.BuiltIn => OperandKind.BuiltIn,
                        Decoration.Location => OperandKind.LiteralInteger,
                        Decoration.SpecId => OperandKind.LiteralSpecConstantOpInteger,
                        Decoration.ArrayStride => OperandKind.LiteralInteger,
                        Decoration.MatrixStride => OperandKind.LiteralInteger,
                        Decoration.UniformId => OperandKind.IdScope,
                        Decoration.Stream => OperandKind.LiteralInteger,
                        Decoration.Component => OperandKind.LiteralInteger,
                        Decoration.Index => OperandKind.LiteralInteger,
                        Decoration.Binding => OperandKind.LiteralInteger,
                        Decoration.DescriptorSet => OperandKind.LiteralInteger,
                        Decoration.Offset => OperandKind.LiteralInteger,
                        Decoration.XfbBuffer => OperandKind.LiteralInteger,
                        Decoration.XfbStride => OperandKind.LiteralInteger,
                        Decoration.FuncParamAttr => OperandKind.FunctionParameterAttribute,
                        Decoration.FPRoundingMode => OperandKind.FPRoundingMode,
                        Decoration.FPFastMathMode => OperandKind.FPFastMathMode,
                        Decoration.LinkageAttributes => OperandKind.LiteralString,
                        Decoration.InputAttachmentIndex => OperandKind.LiteralInteger,
                        Decoration.Alignment => OperandKind.LiteralInteger,
                        Decoration.MaxByteOffset => OperandKind.LiteralInteger,
                        Decoration.AlignmentId => OperandKind.IdRef,
                        Decoration.MaxByteOffsetId => OperandKind.IdRef,
                        Decoration.SecondaryViewportRelativeNV => OperandKind.LiteralInteger,
                        Decoration.CounterBuffer => OperandKind.IdRef,
                        _ => OperandKind.None
                    },
                    Quantifier = OperandQuantifier.One,
                    Words = Operands.Slice(wid, 1)
                },
                _ => throw new NotImplementedException()
            },
            _ => logOp switch
            {
                { Quantifier: OperandQuantifier.One, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef } l
                    => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2)),
                { Quantifier: OperandQuantifier.One, Kind: OperandKind.LiteralString } => new(logOp.Name, OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, Operands[wid..].LengthOfString())),
                { Quantifier: OperandQuantifier.One, Kind: _ } => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1)),
                { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                    => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2)),
                { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.LiteralString }
                    => new(logOp.Name, OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, Operands[wid..].LengthOfString())),
                { Quantifier: OperandQuantifier.ZeroOrOne, Kind: _ }
                    => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, wid < Operands.Length -1 ? Operands.Slice(wid, 1) : []),
                { Quantifier: OperandQuantifier.ZeroOrMore, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                   => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, wid < Operands.Length - 1 ? Operands[wid..] : []),
                { Quantifier: OperandQuantifier.ZeroOrMore, Kind: OperandKind.LiteralString }
                    => throw new Exception("params of strings is not yet implemented"),
                { Quantifier: OperandQuantifier.ZeroOrMore, Kind: _ }
                    => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, wid < Operands.Length - 1 ? Operands[wid..] : []),
                _ => throw new NotImplementedException()
            }
        };
    }

}