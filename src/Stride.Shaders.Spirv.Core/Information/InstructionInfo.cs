using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core;

public record struct OperandKey(Op Op, Decoration? Decoration = null)
{
    public static implicit operator OperandKey(Op op) => new(op);
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
        Info.Add(new(Op.OpDecorate, Decoration.SpecId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "specId")]));
        Info.Add(new(Op.OpDecorate, Decoration.ArrayStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "arrayStride")]));
        Info.Add(new(Op.OpDecorate, Decoration.MatrixStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "matrixStride")]));
        Info.Add(new(Op.OpDecorate, Decoration.BuiltIn), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.BuiltIn, OperandQuantifier.One, "builtin")]));
        Info.Add(new(Op.OpDecorate, Decoration.UniformId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "scopeId")]));
        Info.Add(new(Op.OpDecorate, Decoration.Stream), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "streamNumber")]));
        Info.Add(new(Op.OpDecorate, Decoration.Location), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "location")]));
        Info.Add(new(Op.OpDecorate, Decoration.Index), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "index")]));
        Info.Add(new(Op.OpDecorate, Decoration.DescriptorSet), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "descriptorSet")]));
        Info.Add(new(Op.OpDecorate, Decoration.Offset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "offset")]));
        Info.Add(new(Op.OpDecorate, Decoration.XfbBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "bufferNumber")]));
        Info.Add(new(Op.OpDecorate, Decoration.XfbStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "stride")]));
        Info.Add(new(Op.OpDecorate, Decoration.FPRoundingMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPRoundingMode, OperandQuantifier.One, "roundingMode")]));
        Info.Add(new(Op.OpDecorate, Decoration.FPFastMathMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPFastMathMode, OperandQuantifier.One, "fastMathMode")]));
        Info.Add(new(Op.OpDecorate, Decoration.LinkageAttributes), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "name"), new(OperandKind.LinkageType, OperandQuantifier.One, "linkageType")]));
        Info.Add(new(Op.OpDecorate, Decoration.InputAttachmentIndex), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "inputAttachmentIndex")]));
        Info.Add(new(Op.OpDecorate, Decoration.Alignment), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "alignment")]));
        Info.Add(new(Op.OpDecorate, Decoration.MaxByteOffset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "maxByteOffset")]));
        Info.Add(new(Op.OpDecorate, Decoration.AlignmentId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "alignmentId")]));
        Info.Add(new(Op.OpDecorate, Decoration.MaxByteOffsetId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "maxByteOffsetId")]));
        Info.Add(new(Op.OpDecorate, Decoration.SecondaryViewportRelativeNV), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "viewportIndex")]));
        Info.Add(new(Op.OpDecorate, Decoration.CounterBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "counterBufferId")]));
        Info.Add(new(Op.OpDecorate, Decoration.FuncParamAttr), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FunctionParameterAttribute, OperandQuantifier.One, "functionParameterAttribute")]));
        Info.Add(new(Op.OpDecorateString, Decoration.UserSemantic), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "semanticName")]));

        Info.Add(new(Op.OpMemberDecorate, Decoration.SpecId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "specId")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.ArrayStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "arrayStride")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.MatrixStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "matrixStride")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.BuiltIn), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.BuiltIn, OperandQuantifier.One, "builtin")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.UniformId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "scopeId")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.Stream), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "streamNumber")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.Location), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "location")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.Index), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "index")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.DescriptorSet), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "descriptorSet")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.Offset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "offset")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.XfbBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "bufferNumber")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.XfbStride), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "stride")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.FPRoundingMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPRoundingMode, OperandQuantifier.One, "roundingMode")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.FPFastMathMode), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FPFastMathMode, OperandQuantifier.One, "fastMathMode")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.LinkageAttributes), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "name"), new(OperandKind.LinkageType, OperandQuantifier.One, "linkageType")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.InputAttachmentIndex), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "inputAttachmentIndex")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.Alignment), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "alignment")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.MaxByteOffset), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "maxByteOffset")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.AlignmentId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "alignmentId")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.MaxByteOffsetId), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "maxByteOffsetId")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.SecondaryViewportRelativeNV), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "viewportIndex")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.CounterBuffer), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.IdRef, OperandQuantifier.One, "counterBufferId")]));
        Info.Add(new(Op.OpMemberDecorate, Decoration.FuncParamAttr), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.FunctionParameterAttribute, OperandQuantifier.One, "functionParameterAttribute")]));
        Info.Add(new(Op.OpMemberDecorateString, Decoration.UserSemantic), new(null, [new(OperandKind.IdRef, OperandQuantifier.One, "target"), new(OperandKind.LiteralInteger, OperandQuantifier.One, "accessor"), new(OperandKind.Decoration, OperandQuantifier.One, "decoration"), new(OperandKind.LiteralString, OperandQuantifier.One, "semanticName")]));

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
            Op.OpDecorateString
                or Op.OpDecorate
                or Op.OpDecorateId => (Decoration)instruction.Operands[1],
            Op.OpMemberDecorate
                or Op.OpMemberDecorateString => (Decoration)instruction.Operands[2],
            _ => null
        };
        return GetInfo(new OperandKey(instruction.OpCode, decoration));
    }
}
