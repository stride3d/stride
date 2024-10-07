using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core;


/// <summary>
/// Singleton object containing informations on every spirv instructions, used for spirv parsing.
/// </summary>
public partial class InstructionInfo
{
    public static InstructionInfo Instance { get; } = new();
    Dictionary<SDSLOp, LogicalOperandArray> Info = new();
    InstructionInfo(){}

    internal void Register(SDSLOp op, OperandKind? kind, OperandQuantifier? quantifier, string? name = null, string? spvClass = null)
    {
        if(Info.TryGetValue(op, out var list))
        {
            list.Add(new(kind, quantifier, name));
        }
        else
        {
            Info.Add(op, new(spvClass) { new(kind, quantifier, name)});
        }
    }
    /// <summary>
    /// Gets information for the instruction operation.
    /// </summary>
    /// <param name="op"></param>
    /// <returns></returns>
    public static LogicalOperandArray GetInfo(SDSLOp op)
    {
        return Instance.Info[op];
    }
}
