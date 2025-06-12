using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core;


/// <summary>
/// Object containing information about SPIR-V instructions based on the unified SPIR-V specification.
/// </summary>
public partial class InstructionInfo
{
    Dictionary<(SDSLOp, StorageClass?), int> OrderGroup = new();

    public static ImmutableArray<SDSLOp> SDSLOperators { get; } = ImmutableArray.Create(Enum.GetValues<SDSLOp>().Where(x => x.ToString().Contains("SDSL")).ToArray());
    public static ImmutableArray<SDSLOp> OpTypes { get; } = ImmutableArray.Create(Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpType")).ToArray());

    void InitOrder()
    {
        int group = 0;
        Span<SDSLOp> initSDSL = [
            SDSLOp.OpNop,
            SDSLOp.OpSDSLShader,
            SDSLOp.OpCapability,
            SDSLOp.OpSDSLMixinInherit,
            SDSLOp.OpSDSLCompose
        ];
        foreach(var e in initSDSL)
            OrderGroup[(e, null)] = group;
        
        group++;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpSDSLImport")))
            OrderGroup[(e, null)] = group;
        OrderGroup[(SDSLOp.OpExtension, null)] = group;

        group++;
        OrderGroup[(SDSLOp.OpExtInstImport, null)] = group;
        group++;
        OrderGroup[(SDSLOp.OpMemoryModel, null)] = group;
        group++;
        OrderGroup[(SDSLOp.OpEntryPoint, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpExecutionMode")))
            OrderGroup[(e, null)] = group;

        group++;
        Span<SDSLOp> opDebugSource = [SDSLOp.OpString, SDSLOp.OpSource, SDSLOp.OpSourceExtension, SDSLOp.OpSourceContinued];
        foreach (var e in opDebugSource)
            OrderGroup[(e, null)] = group;

        group++;
        OrderGroup[(SDSLOp.OpName, null)] = group;
        OrderGroup[(SDSLOp.OpSDSLMixinVariable, null)] = group;
        OrderGroup[(SDSLOp.OpMemberName, null)] = group;

        group++;
        OrderGroup[(SDSLOp.OpModuleProcessed, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpDecorate")))
            OrderGroup[(e, null)] = group;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpMemberDecorate")))
            OrderGroup[(e, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpType") || x.ToString().StartsWith("OpConstant") || x.ToString().StartsWith("OpSpec")))
            OrderGroup[(e, null)] = group;

        foreach (var e in Enum.GetValues<StorageClass>().Where(x => x != StorageClass.Function))
            OrderGroup[(SDSLOp.OpVariable, e)] = group;
        OrderGroup[(SDSLOp.OpSDSLIOVariable, null)] = group;

        OrderGroup[(SDSLOp.OpUndef, null)] = group;

        group++;
        OrderGroup[(SDSLOp.OpLine, null)] = group;
        OrderGroup[(SDSLOp.OpNoLine, null)] = group;

        group++;
        group++;
        foreach (var e in Enum.GetValues<SDSLOp>().Except(OrderGroup.Keys.Select(x => x.Item1)))
            OrderGroup[(e, null)] = group;
        OrderGroup[(SDSLOp.OpVariable, StorageClass.Function)] = group;
        group++;
        OrderGroup[(SDSLOp.OpSDSLShaderEnd, null)] = group;
    }
    /// <summary>
    /// Gets the order group for a given instruction, useful for sorting instructions according to the specification.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public static int GetGroupOrder(RefInstruction instruction)
    {
        return GetGroupOrder(instruction.OpCode, instruction.OpCode == SDSLOp.OpVariable ? (StorageClass)instruction.Words[3] : null);
    }
    
    /// <summary>
    /// Gets the order group for a given instruction, useful for sorting instructions according to the specification.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public static int GetGroupOrder(Instruction instruction)
    {
        return GetGroupOrder(instruction.OpCode, instruction.OpCode == SDSLOp.OpVariable ? (StorageClass)instruction.Words[3] : null);
    }
    /// <summary>
    /// Gets the order group for a given instruction and Storage class, useful for sorting instructions according to the specification.
    /// </summary>
    /// <param name="op"></param>
    /// <param name="sc"></param>
    /// <returns></returns>
    public static int GetGroupOrder(SDSLOp op, StorageClass? sc = null)
    {
        return Instance.OrderGroup[(op, sc)];
    }

}
