using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core;



public partial class InstructionInfo
{
    Dictionary<(SDSLOp, StorageClass?), int> OrderGroup = new();

    public static ImmutableArray<SDSLOp> SDSLOperators { get; } = ImmutableArray.Create(Enum.GetValues<SDSLOp>().Where(x => x.ToString().Contains("SDSL")).ToArray());
    public static ImmutableArray<SDSLOp> OpTypes { get; } = ImmutableArray.Create(Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpType")).ToArray());

    void InitOrder()
    {
        OrderGroup[(SDSLOp.OpNop, null)] = 0;
        OrderGroup[(SDSLOp.OpSDSLMixinName, null)] = 0;
        OrderGroup[(SDSLOp.OpCapability, null)] = 0;
        OrderGroup[(SDSLOp.OpSDSLMixinOffset, null)] = 0;
        OrderGroup[(SDSLOp.OpSDSLMixinInherit, null)] = 0;
        OrderGroup[(SDSLOp.OpSDSLCompose, null)] = 0;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpSDSLImport")))
            OrderGroup[(e, null)] = 1;
        OrderGroup[(SDSLOp.OpExtension, null)] = 1;
        OrderGroup[(SDSLOp.OpExtInstImport, null)] = 2;
        OrderGroup[(SDSLOp.OpMemoryModel, null)] = 3;
        OrderGroup[(SDSLOp.OpEntryPoint, null)] = 4;

        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpExecutionMode")))
            OrderGroup[(e, null)] = 5;

        foreach (var e in new SDSLOp[] { SDSLOp.OpString, SDSLOp.OpSource, SDSLOp.OpSourceExtension, SDSLOp.OpSourceContinued })
            OrderGroup[(e, null)] = 6;

        OrderGroup[(SDSLOp.OpName, null)] = 7;
        OrderGroup[(SDSLOp.OpSDSLMixinVariable, null)] = 7;
        OrderGroup[(SDSLOp.OpMemberName, null)] = 7;

        OrderGroup[(SDSLOp.OpModuleProcessed, null)] = 8;

        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpDecorate")))
            OrderGroup[(e, null)] = 9;
        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpMemberDecorate")))
            OrderGroup[(e, null)] = 9;

        foreach (var e in Enum.GetValues<SDSLOp>().Where(x => x.ToString().StartsWith("OpType") || x.ToString().StartsWith("OpConstant") || x.ToString().StartsWith("OpSpec")))
            OrderGroup[(e, null)] = 10;

        foreach (var e in Enum.GetValues<StorageClass>().Where(x => x != StorageClass.Function))
            OrderGroup[(SDSLOp.OpVariable, e)] = 10;
        OrderGroup[(SDSLOp.OpSDSLIOVariable, null)] = 10;

        OrderGroup[(SDSLOp.OpUndef, null)] = 10;
        OrderGroup[(SDSLOp.OpLine, null)] = 11;
        OrderGroup[(SDSLOp.OpNoLine, null)] = 11;

        foreach (var e in Enum.GetValues<SDSLOp>().Except(OrderGroup.Keys.Select(x => x.Item1)))
            OrderGroup[(e, null)] = 13;
        OrderGroup[(SDSLOp.OpVariable, StorageClass.Function)] = 13;
        OrderGroup[(SDSLOp.OpSDSLMixinEnd, null)] = 14;
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
    public static int GetGroupOrder(MutRefInstruction instruction)
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
        return GetGroupOrder(instruction.OpCode, instruction.OpCode == SDSLOp.OpVariable ? (StorageClass)instruction.Words.Span[3] : null);
    }

    public static int GetGroupOrder(SDSLOp op, StorageClass? sc = null)
    {
        return Instance.OrderGroup[(op, sc)];
    }

}
