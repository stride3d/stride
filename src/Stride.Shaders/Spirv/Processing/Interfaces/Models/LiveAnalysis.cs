using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Models;

internal class MethodInfo
{
    /// <summary>
    /// Used during current stage being processed?
    /// </summary>
    public bool UsedThisStage { get => field; set { field = value; UsedAnyStage |= value; } }
    /// <summary>
    /// Used at all (in any stage)
    /// </summary>
    public bool UsedAnyStage { get; private set; }

    public List<OpData>? OriginalMethodCode { get; set; }
    public int? ThisStageMethodId { get; set; }

    // True if the method depends on STREAMS type (also if used by any OpFunctionCall recursively)
    public bool HasStreamAccess { get; internal set; }
}

internal class LiveAnalysis
{
    public Dictionary<int, MethodInfo> ReferencedMethods { get; } = new();

    public HashSet<int> ExtraReferencedMethods { get; } = new();

    public MethodInfo GetOrCreateMethodInfo(int functionId)
    {
        if (!ReferencedMethods.TryGetValue(functionId, out MethodInfo methodInfo))
            ReferencedMethods.Add(functionId, methodInfo = new MethodInfo());

        return methodInfo;
    }

    public bool MarkMethodUsed(int functionId)
    {
        var methodInfo = GetOrCreateMethodInfo(functionId);

        var previousValue = methodInfo.UsedThisStage;
        methodInfo.UsedThisStage = true;
        // Returns tree when added first time
        return !previousValue;
    }
}
