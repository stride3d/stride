using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// An instruction operands enumerator, useful for parsing instructions
/// </summary>
public ref struct OpDataEnumerator
{
    static readonly OperandKind[] pairs = [.. Enum.GetValues<OperandKind>().Where(x => x.ToString().StartsWith("Pair"))];
    readonly Span<int> instruction;
    readonly Span<int> Operands => instruction[1..];
    readonly Op OpCode => (Op)(instruction[0] & 0xFFFF);
    readonly LogicalOperandArray logicalOperands;
    int wid;
    int oid;

    public OpDataEnumerator(Span<int> instruction)
    {
        this.instruction = instruction;
        logicalOperands = InstructionInfo.GetInfo(OpCode);
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
            return result && wid <= Operands.Length && oid < logicalOperands.Count;
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
                    => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1)),
                { Quantifier: OperandQuantifier.ZeroOrMore }
                    => pairs.Contains(logOp.Kind ?? OperandKind.None)
                        ? new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2))
                        : new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1)),
                _ => throw new NotImplementedException()
            }
        };
    }

}



// public ref struct OpDataEnumerator
// {
//     static readonly OperandKind[] pairs = [.. Enum.GetValues<OperandKind>().Where(x => x.ToString().StartsWith("Pair"))];
//     readonly Span<int> instruction;
//     readonly Span<int> Operands => instruction[1..];
//     readonly Op OpCode => (Op)(instruction[0] & 0xFFFF);
//     readonly LogicalOperandArray logicalOperands;
//     int wid;
//     int oid;

//     public OpDataEnumerator(Span<int> instruction)
//     {
//         this.instruction = instruction;
//         logicalOperands = InstructionInfo.GetInfo(OpCode);
//         oid = -1;
//         wid = 0;
//     }

//     public SpvOperand Current => ParseCurrent();

//     public bool MoveNext()
//     {
//         if (oid < 0)
//         {
//             oid = 0;
//             if (logicalOperands[0].Kind == OperandKind.None)
//                 return false;
//             return true;
//         }
//         else
//         {

//             var logOp = logicalOperands[oid];

//             if (OpCode == Op.OpDecorate)
//             {
//                 if (oid == 0)
//                 {
//                     wid += 1;
//                     oid += 1;
//                     return true;
//                 }
//                 else if (oid > 0)
//                 {
//                     var builtin = (Decoration)Operands[1];
//                     bool has2Extra = builtin == Decoration.LinkageAttributes;
//                     bool has1Extra =
//                         builtin == Decoration.BuiltIn
//                         || builtin == Decoration.Location
//                         || builtin == Decoration.SpecId
//                         || builtin == Decoration.ArrayStride
//                         || builtin == Decoration.MatrixStride
//                         || builtin == Decoration.UniformId
//                         || builtin == Decoration.Stream
//                         || builtin == Decoration.Component
//                         || builtin == Decoration.Index
//                         || builtin == Decoration.Binding
//                         || builtin == Decoration.DescriptorSet
//                         || builtin == Decoration.Offset
//                         || builtin == Decoration.XfbBuffer
//                         || builtin == Decoration.XfbStride
//                         || builtin == Decoration.FuncParamAttr
//                         || builtin == Decoration.FPRoundingMode
//                         || builtin == Decoration.FPFastMathMode
//                         || builtin == Decoration.LinkageAttributes
//                         || builtin == Decoration.InputAttachmentIndex
//                         || builtin == Decoration.Alignment
//                         || builtin == Decoration.MaxByteOffset
//                         || builtin == Decoration.AlignmentId
//                         || builtin == Decoration.MaxByteOffsetId
//                         || builtin == Decoration.SecondaryViewportRelativeNV
//                         || builtin == Decoration.CounterBuffer;
//                     if (has1Extra && oid == 1 && !has2Extra)
//                     {
//                         wid += 1;
//                         oid += 1;
//                     }
//                     else if (has2Extra)
//                     {
//                         throw new NotImplementedException();
//                     }
//                     else
//                     {
//                         return false;
//                     }

//                 }

//                 oid += 1;
//                 if (oid > 2)
//                     return false;
//                 else
//                     return wid < Operands.Length;
//             }
//             else if (logOp.Quantifier == OperandQuantifier.One)
//             {
//                 if (logOp.Kind == OperandKind.LiteralString)
//                 {
//                     while (!Operands[wid].HasEndString())
//                         wid += 1;
//                     wid += 1;
//                 }
//                 else if (pairs.Contains(logOp.Kind ?? throw new Exception("kind is inexistent")))
//                     wid += 2;
//                 else
//                     wid += 1;
//                 oid += 1;

//             }
//             else if (logOp.Quantifier == OperandQuantifier.ZeroOrOne)
//             {
//                 if (
//                     pairs.Contains(logOp.Kind ?? throw new Exception("kind is inexistent"))
//                     && wid < Operands.Length - 1
//                 )
//                 {
//                     wid += 2;
//                 }
//                 else if (
//                     logOp.Kind == OperandKind.LiteralString
//                     && wid < Operands.Length
//                 )
//                 {
//                     while (!Operands[wid].HasEndString())
//                         wid += 1;
//                     wid += 1;
//                 }
//                 else if (wid < Operands.Length)
//                     wid += 1;
//                 oid += 1;

//             }
//             else if (logOp.Quantifier == OperandQuantifier.ZeroOrMore)
//             {
//                 if (logOp.Kind == OperandKind.LiteralString)
//                     throw new NotImplementedException("params of strings is not yet implemented");
//                 else if (
//                     pairs.Contains(logOp.Kind ?? throw new Exception())
//                     && wid < Operands.Length - 2
//                 )
//                     wid += 2;
//                 else if (wid < Operands.Length - 1)
//                     wid += 1;
//                 else
//                     oid += 1;

//             }
//             if (oid >= logicalOperands.Count)
//                 return false;
//             return wid < Operands.Length;
//         }

//     }

//     public SpvOperand ParseCurrent()
//     {
//         var logOp = logicalOperands[oid];
//         if (OpCode == Op.OpDecorate)
//         {
//             SpvOperand result = new();
//             if (oid == 0)
//                 result = new(OperandKind.IdRef, OperandQuantifier.One, Operands.Slice(wid, 1));
//             else if (oid == 1)
//                 result = new(OperandKind.Decoration, OperandQuantifier.One, Operands.Slice(wid, 1));
//             else if (oid == 2)
//             {
//                 result = result with
//                 {
//                     Kind = (Decoration)Operands[1] switch
//                     {
//                         Decoration.BuiltIn => OperandKind.BuiltIn,
//                         Decoration.Location => OperandKind.LiteralInteger,
//                         Decoration.SpecId => OperandKind.LiteralSpecConstantOpInteger,
//                         Decoration.ArrayStride => OperandKind.LiteralInteger,
//                         Decoration.MatrixStride => OperandKind.LiteralInteger,
//                         Decoration.UniformId => OperandKind.IdScope,
//                         Decoration.Stream => OperandKind.LiteralInteger,
//                         Decoration.Component => OperandKind.LiteralInteger,
//                         Decoration.Index => OperandKind.LiteralInteger,
//                         Decoration.Binding => OperandKind.LiteralInteger,
//                         Decoration.DescriptorSet => OperandKind.LiteralInteger,
//                         Decoration.Offset => OperandKind.LiteralInteger,
//                         Decoration.XfbBuffer => OperandKind.LiteralInteger,
//                         Decoration.XfbStride => OperandKind.LiteralInteger,
//                         Decoration.FuncParamAttr => OperandKind.FunctionParameterAttribute,
//                         Decoration.FPRoundingMode => OperandKind.FPRoundingMode,
//                         Decoration.FPFastMathMode => OperandKind.FPFastMathMode,
//                         Decoration.LinkageAttributes => OperandKind.LiteralString,
//                         Decoration.InputAttachmentIndex => OperandKind.LiteralInteger,
//                         Decoration.Alignment => OperandKind.LiteralInteger,
//                         Decoration.MaxByteOffset => OperandKind.LiteralInteger,
//                         Decoration.AlignmentId => OperandKind.IdRef,
//                         Decoration.MaxByteOffsetId => OperandKind.IdRef,
//                         Decoration.SecondaryViewportRelativeNV => OperandKind.LiteralInteger,
//                         Decoration.CounterBuffer => OperandKind.IdRef,
//                         _ => OperandKind.None
//                     }
//                 };
//             }
//             return result;

//         }
//         else if (logOp.Quantifier != OperandQuantifier.ZeroOrMore)
//         {
//             if (logOp.Kind == OperandKind.LiteralString)
//             {
//                 var length = 0;
//                 while (!Operands[wid + length].HasEndString())
//                     length += 1;
//                 length += 1;
//                 var result = new SpvOperand(logOp.Name, OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, length));

//                 return result;
//             }
//             else if (pairs.Contains(logOp.Kind ?? throw new NotImplementedException("")))
//             {
//                 var result = new SpvOperand(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2));
//                 return result;
//             }
//             else
//                 return new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1));
//         }
//         else
//         {
//             if (pairs.Contains(logOp.Kind ?? OperandKind.None))
//                 return new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2));
//             else
//                 return new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1));
//         }
//     }

// }