using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core;


public enum OperandWordSize
{
    One,
    Two,
    Variable,
    Rest
}
public readonly partial record struct LogicalOperand
{
    public int GetWordSize()
    {
        return Kind switch
        {
            OperandKind.PackedVectorFormat
            or OperandKind.ImageOperands
            or OperandKind.FPFastMathMode
            or OperandKind.SelectionControl
            or OperandKind.LoopControl
            or OperandKind.FunctionControl
            or OperandKind.MemorySemantics
            or OperandKind.MemoryAccess
            or OperandKind.KernelProfilingInfo
            or OperandKind.RayFlags
            or OperandKind.FragmentShadingRate
            or OperandKind.SourceLanguage
            or OperandKind.ExecutionModel
            or OperandKind.AddressingModel
            or OperandKind.MemoryModel
            or OperandKind.ExecutionMode
            or OperandKind.StorageClass
            or OperandKind.Dim
            or OperandKind.SamplerAddressingMode
            or OperandKind.SamplerFilterMode
            or OperandKind.ImageFormat
            or OperandKind.ImageChannelOrder
            or OperandKind.ImageChannelDataType
            or OperandKind.FPRoundingMode
            or OperandKind.LinkageType
            or OperandKind.AccessQualifier
            or OperandKind.FunctionParameterAttribute
            or OperandKind.Decoration
            or OperandKind.BuiltIn
            or OperandKind.Scope
            or OperandKind.GroupOperation
            or OperandKind.KernelEnqueueFlags
            or OperandKind.Capability
            or OperandKind.RayQueryIntersection
            or OperandKind.RayQueryCommittedIntersectionType
            or OperandKind.RayQueryCandidateIntersectionType
            or OperandKind.IdResultType
            or OperandKind.IdResult
            or OperandKind.IdMemorySemantics
            or OperandKind.IdScope
            or OperandKind.IdRef
            or OperandKind.LiteralInteger => 1,
            OperandKind.LiteralString => -1,
            OperandKind.LiteralContextDependentNumber => -1,
            OperandKind.LiteralExtInstInteger => 1,
            OperandKind.LiteralSpecConstantOpInteger => 1,
            OperandKind.PairLiteralIntegerIdRef => 2,
            OperandKind.PairIdRefLiteralInteger => 2,
            OperandKind.PairIdRefIdRef => 2,
            _ => throw new NotImplementedException("Operand kind not recognized")
        };
    }
}
