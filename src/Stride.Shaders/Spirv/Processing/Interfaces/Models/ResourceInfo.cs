namespace Stride.Shaders.Spirv.Processing.Interfaces.Models;

internal class ResourceInfo(string name)
{
    public string Name { get; } = name;

    public ResourceGroup ResourceGroup { get; set; }

    /// <summary>
    /// Used during current stage being processed?
    /// </summary>
    public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
    /// <summary>
    /// Used at all (in any stage)
    /// </summary>
    public bool UsedAnyStage { get; private set; }
}

internal record class ResourceGroup
{
    public bool Used { get; set; }
    public string Name { get; set; }
    public string? LogicalGroup { get; set; }
    public List<ResourceInfo> Resources { get; } = new();
}

internal record class CBufferInfo(string name)
{
    public string Name { get; } = name;

    public string? LogicalGroup { get; set; }

    /// <summary>
    /// Used during current stage being processed?
    /// </summary>
    public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
    /// <summary>
    /// Used at all (in any stage)
    /// </summary>
    public bool UsedAnyStage { get; private set; }
}

internal class LogicalGroupInfo
{
    public List<ResourceGroup> Resources { get; } = new();
    public List<CBufferInfo> CBuffers { get; } = new();
}
