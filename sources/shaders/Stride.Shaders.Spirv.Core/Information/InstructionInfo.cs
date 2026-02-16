using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core;


/// <summary>
/// Singleton object containing informations on every spirv instructions, used for spirv parsing.
/// </summary>
public partial class InstructionInfo
{
    public static InstructionInfo Instance { get; } = new();
    readonly Dictionary<Op, LogicalOperandArray> Info = [];

    /// <summary>
    /// Register information about a SPIR-V instruction
    /// </summary>
    /// <param name="op"></param>
    /// <param name="kind"></param>
    /// <param name="quantifier"></param>
    /// <param name="name"></param>
    /// <param name="spvClass"></param>
    internal void Register(Op op, OperandKind? kind, OperandQuantifier? quantifier, string? name = null, string? spvClass = null, OperandParameters? parameters = null)
    {
        if (Info.TryGetValue(op, out var list))
            list.Add(new(name, kind, quantifier, parameters ?? []));
        else
            Info.Add(op, new(spvClass, [new(name, kind, quantifier, parameters ?? [])]));
    }

    public static LogicalOperandArray GetInfo(Op op)
        => Instance.Info[op];
    public static LogicalOperandArray GetInfo(Span<int> words) 
        => GetInfo((Op)(words[0] & 0xFFFF));

    public static LogicalOperandArray GetInfo(Instruction instruction) 
        => GetInfo(instruction.Words);
        
    public static LogicalOperandArray GetInfo(OpData instruction)
        => GetInfo(instruction.Op);
}