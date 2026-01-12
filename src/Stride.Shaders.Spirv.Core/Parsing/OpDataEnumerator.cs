using System.Reflection.Metadata.Ecma335;
using Stride.Shaders.Spirv.Core.Buffers;
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
    int pid;
    int startOperand;

    public OpDataEnumerator(Span<int> instruction)
    {
        this.instruction = instruction;
        logicalOperands = InstructionInfo.GetInfo(instruction);
        // If OpSpecConstantOp, there is an inner OpCode which will define additional inner operands
        if (OpCode == Op.OpSpecConstantOp)
        {
            var combinedLogicalOperands = new List<LogicalOperand>();
            foreach (var o in logicalOperands)
                combinedLogicalOperands.Add(o);
            var innerOpCode = (Op)instruction[3];
            var innerLogicalOperands = InstructionInfo.GetInfo(innerOpCode);
            // Start from third operand (skip ResultType/ResultId)
            for (int i = 2; i < innerLogicalOperands.Count; ++i)
            {
                combinedLogicalOperands.Add(innerLogicalOperands[i]);
            }
            logicalOperands = new LogicalOperandArray(logicalOperands.ClassName, combinedLogicalOperands);
        }
        oid = -1;
        pid = -1;
        wid = 0;
    }

    public SpvOperand Current => ParseCurrent();

    public bool FindOperandInfo(OperandParameters p, ParameterizedOperandKey key, out ParameterizedOperand[] operands)
    {
        if (p.TryGetValue(key, out operands))
            return true;

        // ImageOperands is used as a flag. Each flag set will expect corresponding operand(s) (flags should be tested in ascending order)
        // TODO: Does it apply to other operand kinds as well?
        if (key.Kind == OperandKind.ImageOperands)
        {
            // In case it's run multithreaded
            lock (p)
            {
                // First time we encounter this combination, build it using flags
                var operandsList = new List<ParameterizedOperand>();
                foreach (var parameter in p)
                {
                    if ((parameter.Key.Value & key.Value) != 0)
                    {
                        operandsList.AddRange(parameter.Value);
                    }
                }

                operands = operandsList.ToArray();

                // Add to cache for next query
                p.Add(key, operands);
                return true;
            }
        }

        return false;
    }

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
            (int newWid, int newOid, int newPid, startOperand) = logOp switch
            {
                { Parameters: OperandParameters { Count: > 0 } p } when pid == -1 && p.ContainsKey(new(logOp.Kind ?? OperandKind.None, Operands[wid])) && p[new(logOp.Kind ?? OperandKind.None, Operands[wid])].Length > 0 =>
                    (wid + 1, oid, 0, wid),
                { Parameters: OperandParameters { Count: > 0 } p } when p.ContainsKey(new(logOp.Kind ?? OperandKind.None, Operands[wid])) && pid < p[new(logOp.Kind ?? OperandKind.None, Operands[startOperand])].Length =>
                    p[new(logOp.Kind ?? OperandKind.None, Operands[startOperand])][pid] switch
                    {
                        { Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef } => (wid + 2, oid, pid + 1, startOperand),
                        { Kind: OperandKind.LiteralString } => (wid + Operands[wid..].LengthOfString(), oid, pid + 1, startOperand),
                        { Kind: _ } => (wid + 1, oid, pid + 1, startOperand)
                    },
                { Quantifier: OperandQuantifier.One, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                    => (wid + 2, oid + 1, -1, -1),
                { Quantifier: OperandQuantifier.One, Kind: OperandKind.LiteralString } => (wid + Operands[wid..].LengthOfString(), oid + 1, -1, -1),
                { Quantifier: OperandQuantifier.One, Kind: _ } => (wid + 1, oid + 1, -1, -1),
                { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                    => (wid + (wid < Operands.Length - 2 ? 2 : 0), oid + (wid < Operands.Length - 2 ? 1 : 0), -1, -1),
                { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.LiteralString }
                    => (wid + (wid < Operands.Length - 1 ? Operands[wid..].LengthOfString() : 0), oid + (wid < Operands.Length ? 1 : 0), -1, -1),
                { Quantifier: OperandQuantifier.ZeroOrOne, Kind: _ }
                    => (wid + (wid < Operands.Length ? 1 : 0), oid + (wid < Operands.Length ? 1 : 0), -1, -1),
                { Quantifier: OperandQuantifier.ZeroOrMore }
                    => (wid < Operands.Length - 1 ? Operands.Length : wid, oid + (wid < Operands.Length - 1 ? 0 : 1), -1, -1),
                _ => throw new NotImplementedException($"Couldn't handle operand {logOp}")
            };
            wid = newWid;
            oid = newOid;
            pid = newPid;
            // Reasons to return false : 
            // - no operands left
            // - current operand has no kind (i.e. None)
            return !(wid >= Operands.Length || oid >= logicalOperands.Count);

        }
    }

    public SpvOperand ParseCurrent()
    {
        var logOp = logicalOperands[oid];

        return logOp switch
        {
            { Parameters: OperandParameters { Count: > 0 } } when pid == -1 =>
                new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1), wid, true),
            { Parameters: OperandParameters { Count: > 0 } p } when p.ContainsKey(new(logOp.Kind ?? OperandKind.None, Operands[startOperand])) && pid < p[new(logOp.Kind ?? OperandKind.None, Operands[startOperand])].Length =>
            p[new(logOp.Kind ?? OperandKind.None, Operands[startOperand])][pid] switch
            {
                { Name: string n, Kind: OperandKind k } when k.ToString().StartsWith("Pair") => new(n, k, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2), wid),
                { Name: string n, Kind: OperandKind.LiteralString } => new(n, OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, Operands[wid..].LengthOfString()), wid),
                { Name: string n, Kind: OperandKind k } => new(n, k, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1), wid),
                _ => throw new NotImplementedException($"Couldn't handle operand kind {logOp.Kind}")
            },
            { Quantifier: OperandQuantifier.One, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef } l
                => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2), wid),
            { Quantifier: OperandQuantifier.One, Kind: OperandKind.LiteralString } => new(logOp.Name, OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, Operands[wid..].LengthOfString()), wid),
            { Quantifier: OperandQuantifier.One, Kind: _ } => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 1), wid),
            { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, 2), wid),
            { Quantifier: OperandQuantifier.ZeroOrOne, Kind: OperandKind.LiteralString }
                => new(logOp.Name, OperandKind.LiteralString, logOp.Quantifier ?? OperandQuantifier.One, Operands.Slice(wid, Operands[wid..].LengthOfString()), wid),
            { Quantifier: OperandQuantifier.ZeroOrOne, Kind: _ }
                => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, wid < Operands.Length ? Operands.Slice(wid, 1) : []),
            { Quantifier: OperandQuantifier.ZeroOrMore, Kind: OperandKind.PairIdRefIdRef or OperandKind.PairIdRefLiteralInteger or OperandKind.PairLiteralIntegerIdRef }
                => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, wid < Operands.Length ? Operands[wid..] : [], wid),
            { Quantifier: OperandQuantifier.ZeroOrMore, Kind: OperandKind.LiteralString }
                => throw new Exception("params of strings is not yet implemented"),
            { Quantifier: OperandQuantifier.ZeroOrMore, Kind: _ }
                => new(logOp.Name, logOp.Kind ?? OperandKind.None, logOp.Quantifier ?? OperandQuantifier.One, wid < Operands.Length ? Operands[wid..] : [], wid),
            _ => throw new NotImplementedException()

        };
    }

}