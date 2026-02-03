using Stride.Shaders.Core;

namespace Stride.Shaders.Spirv.Processing.InterfaceProcessorInternal.Models;

internal class VariableInfo(string name, PointerType type, int variableId)
{
    public string Name { get; } = name;
    public PointerType Type { get; } = type;

    public int VariableId { get; } = variableId;
    public int? VariableMethodInitializerId { get; set; }

    /// <summary>
    /// Used during current stage being processed?
    /// </summary>
    public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
    /// <summary>
    /// Used at all (in any stage)
    /// </summary>
    public bool UsedAnyStage { get; private set; }
}
