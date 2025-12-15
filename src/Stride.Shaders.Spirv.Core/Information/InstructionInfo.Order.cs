using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core;


/// <summary>
/// Object containing information about SPIR-V instructions based on the unified SPIR-V specification.
/// </summary>
public partial class InstructionInfo
{
    Dictionary<(Op, StorageClass?), int> OrderGroup = new();

    public static ImmutableArray<Op> SDSLOperators { get; } = ImmutableArray.Create(Enum.GetValues<Op>().Where(x => x.ToString().Contains("SDSL")).ToArray());
    public static ImmutableArray<Op> OpTypes { get; } = ImmutableArray.Create(Enum.GetValues<Op>().Where(x => x.ToString().StartsWith("OpType")).ToArray());

    void InitOrder()
    {
        int group = 0;
        Span<Op> initSDSL = [
            Op.OpNop,
            Op.OpSDSLShader,
            Op.OpSDSLEffect,
            Op.OpCapability,
            Op.OpSDSLCompose
        ];
        foreach (var e in initSDSL)
            OrderGroup[(e, null)] = group;

        group++;
        OrderGroup[(Op.OpExtension, null)] = group;

        group++;
        OrderGroup[(Op.OpExtInstImport, null)] = group;
        group++;
        OrderGroup[(Op.OpMemoryModel, null)] = group;
        group++;
        OrderGroup[(Op.OpEntryPoint, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<Op>().Where(x => x.ToString().StartsWith("OpExecutionMode")))
            OrderGroup[(e, null)] = group;

        group++;
        Span<Op> opDebugSource = [Op.OpString, Op.OpSource, Op.OpSourceExtension, Op.OpSourceContinued];
        foreach (var e in opDebugSource)
            OrderGroup[(e, null)] = group;

        group++;
        OrderGroup[(Op.OpName, null)] = group;
        OrderGroup[(Op.OpMemberName, null)] = group;

        group++;
        OrderGroup[(Op.OpModuleProcessed, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<Op>().Where(x => x.ToString().StartsWith("OpDecorate")))
            OrderGroup[(e, null)] = group;
        foreach (var e in Enum.GetValues<Op>().Where(x => x.ToString().StartsWith("OpMemberDecorate")))
            OrderGroup[(e, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<Op>().Where(x => x.ToString().StartsWith("OpType") || x.ToString().StartsWith("OpConstant") || x.ToString().StartsWith("OpSpec") || x.ToString().StartsWith("OpSDSLImport") || x == Op.OpSDSLGenericParameter))
            OrderGroup[(e, null)] = group;

        group++;
        OrderGroup[(Op.OpSDSLMixinInherit, null)] = group;

        group++;
        foreach (var e in Enum.GetValues<StorageClass>().Where(x => x != StorageClass.Function))
            OrderGroup[(Op.OpVariable, e)] = group;
        foreach (var e in Enum.GetValues<StorageClass>().Where(x => x != StorageClass.Function))
            OrderGroup[(Op.OpVariableSDSL, e)] = group;

        OrderGroup[(Op.OpUndef, null)] = group;

        group++;
        OrderGroup[(Op.OpLine, null)] = group;
        OrderGroup[(Op.OpNoLine, null)] = group;

        group++;
        group++;
        foreach (var e in Enum.GetValues<Op>().Except(OrderGroup.Keys.Select(x => x.Item1)))
            OrderGroup[(e, null)] = group;
        OrderGroup[(Op.OpVariable, StorageClass.Function)] = group;
        group++;
        OrderGroup[(Op.OpSDSLShaderEnd, null)] = group;
    }
    /// <summary>
    /// Gets the order group for a given instruction, useful for sorting instructions according to the specification.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public static int GetGroupOrder(Instruction instruction)
    {
        return GetGroupOrder(instruction.OpCode, instruction.OpCode == Op.OpVariable || instruction.OpCode == Op.OpVariableSDSL ? (StorageClass)instruction.Words[3] : null);
    }
    public static int GetGroupOrder(Buffers.OpData instruction)
    {
        return GetGroupOrder(instruction.Op, instruction.Op == Op.OpVariable || instruction.Op == Op.OpVariableSDSL ? (StorageClass)instruction.Memory.Span[3] : null);
    }

    /// <summary>
    /// Gets the order group for a given instruction and Storage class, useful for sorting instructions according to the specification.
    /// </summary>
    /// <param name="op"></param>
    /// <param name="sc"></param>
    /// <returns></returns>
    public static int GetGroupOrder(Op op, StorageClass? sc = null)
    {
        return Instance.OrderGroup[(op, sc)];
    }

}
