using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

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
        Info.Add(new(SDSLOp.OpDecorate, Decoration.SpecId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "specId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.ArrayStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "arrayStride")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.MatrixStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "matrixStride")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.BuiltIn), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.BuiltIn, OperandQuantifier.One, "builtin")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.UniformId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "scopeId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Stream), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "streamNumber")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Location), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "location")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Index), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "index")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.DescriptorSet), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "descriptorSet")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Offset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "offset")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.XfbBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "bufferNumber")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.XfbStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "stride")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.FPRoundingMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPRoundingMode, OperandQuantifier.One, "roundingMode")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.FPFastMathMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPFastMathMode, OperandQuantifier.One, "fastMathMode")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.LinkageAttributes), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "name"), new(OperandKind.LinkageType, OperandQuantifier.One, "linkageType")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.InputAttachmentIndex), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "inputAttachmentIndex")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.Alignment), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "alignment")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.MaxByteOffset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "maxByteOffset")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.AlignmentId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "alignmentId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.MaxByteOffsetId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "maxByteOffsetId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.SecondaryViewportRelativeNV), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "viewportIndex")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.CounterBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "counterBufferId")]));
        Info.Add(new(SDSLOp.OpDecorate, Decoration.FuncParamAttr), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FunctionParameterAttribute, OperandQuantifier.One, "functionParameterAttribute")]));
        Info.Add(new(SDSLOp.OpDecorateString, Decoration.UserSemantic), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "semanticName")]));

        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.SpecId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "specId")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.ArrayStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "arrayStride")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.MatrixStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "matrixStride")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.BuiltIn), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.BuiltIn, OperandQuantifier.One, "builtin")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.UniformId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "scopeId")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.Stream), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "streamNumber")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.Location), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "location")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.Index), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "index")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.DescriptorSet), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "descriptorSet")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.Offset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "offset")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.XfbBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "bufferNumber")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.XfbStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "stride")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.FPRoundingMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPRoundingMode, OperandQuantifier.One, "roundingMode")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.FPFastMathMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPFastMathMode, OperandQuantifier.One, "fastMathMode")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.LinkageAttributes), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "name"), new(OperandKind.LinkageType, OperandQuantifier.One, "linkageType")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.InputAttachmentIndex), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "inputAttachmentIndex")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.Alignment), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "alignment")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.MaxByteOffset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "maxByteOffset")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.AlignmentId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "alignmentId")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.MaxByteOffsetId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "maxByteOffsetId")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.SecondaryViewportRelativeNV), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "viewportIndex")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.CounterBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "counterBufferId")]));
        Info.Add(new(SDSLOp.OpMemberDecorate, Decoration.FuncParamAttr), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FunctionParameterAttribute, OperandQuantifier.One, "functionParameterAttribute")]));
        Info.Add(new(SDSLOp.OpMemberDecorateString, Decoration.UserSemantic), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "semanticName")]));

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
        if(op.Decoration is not null && !Instance.Info.ContainsKey(op))
            return Instance.Info[op with { Decoration = null }];
        return Instance.Info[op];
    }
    public static LogicalOperandArray GetInfo(Instruction instruction)
    {
        Decoration? decoration = instruction.OpCode switch
        {
            SDSLOp.OpDecorateString
                or SDSLOp.OpDecorate
                or SDSLOp.OpDecorateId => (Decoration)instruction.Operands[1],
            SDSLOp.OpMemberDecorate
                or SDSLOp.OpMemberDecorateString => (Decoration)instruction.Operands[2],
            _ => null
        };
        return GetInfo(new OperandKey(instruction.OpCode, decoration));
    }
}
