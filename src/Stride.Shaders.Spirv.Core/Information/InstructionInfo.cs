using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core;

public record struct OperandKey(SDSLOp Op, Decoration? Decoration = null)
{
    public static implicit operator OperandKey(SDSLOp op) => new(op);
}


/// <summary>
/// Singleton object containing informations on every spirv instructions, used for spirv parsing.
/// </summary>
public partial class InstructionInfo
{
    public static InstructionInfo Instance { get; } = new();
    readonly Dictionary<OperandKey, LogicalOperandArray> Info = [];
    InstructionInfo()
    {
        Info.Add(new(SDSLOp.OpDecorate, Decoration.SpecId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "specId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.ArrayStride), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "arrayStride")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.MatrixStride), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "matrixStride")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.BuiltIn), new(null, [new(OperandKind.BuiltIn, OperandQuantifier.One, "builtin")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.UniformId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "scopeId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Stream), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "streamNumber")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Location), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "location")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Index), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "index")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.DescriptorSet), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "descriptorSet")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Offset), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "offset")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.XfbBuffer), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "bufferNumber")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.XfbStride), new(null, [new(OperandKind.LiteralInteger, OperandQuantifier.One, "stride")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.FPRoundingMode), new(null, [new(OperandKind.FPRoundingMode, OperandQuantifier.One, "roundingMode")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.FPFastMathMode), new(null, [new(OperandKind.FPFastMathMode, OperandQuantifier.One, "fastMathMode")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.LinkageAttributes), new(null, [new(OperandKind.LiteralString, OperandQuantifier.One, "name"), new(OperandKind.LinkageType, OperandQuantifier.One, "linkageType")]));
        #warning needs more information
    }
    /// <summary>
    /// Register information about a SPIR-V instruction
    /// </summary>
    /// <param name="op"></param>
    /// <param name="kind"></param>
    /// <param name="quantifier"></param>
    /// <param name="name"></param>
    /// <param name="spvClass"></param>
    internal void Register(OperandKey op, OperandKind? kind, OperandQuantifier? quantifier, string? name = null, string? spvClass = null)
    {
        if (Info.TryGetValue(op, out var list))
            list.Add(new(kind, quantifier, name));
        else
            Info.Add(op, new(spvClass, [new(kind, quantifier, name)]));
    }
    /// <summary>
    /// Gets information for the instruction operation.
    /// </summary>
    /// <param name="op"></param>
    /// <returns></returns>
    public static LogicalOperandArray GetInfo(OperandKey op)
    {
        return Instance.Info[op];
    }
}
