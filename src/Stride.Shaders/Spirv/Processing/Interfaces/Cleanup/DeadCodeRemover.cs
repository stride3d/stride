using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces.Analysis;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Cleanup;

/// <summary>
/// Handles removal of unreferenced code including methods, variables, resources, and types.
/// </summary>
internal static class DeadCodeRemover
{
    /// <summary>
    /// Resets the UsedThisStage flag for all variables, resources, cbuffers, and methods
    /// in preparation for analyzing the next shader stage.
    /// </summary>
    public static void ResetUsedThisStage(AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
    {
        foreach (var variable in analysisResult.Variables)
            variable.Value.UsedThisStage = false;
        foreach (var resource in analysisResult.Resources)
            resource.Value.UsedThisStage = false;
        foreach (var cbuffer in analysisResult.CBuffers)
            cbuffer.Value.UsedThisStage = false;
        foreach (var method in liveAnalysis.ReferencedMethods)
        {
            method.Value.UsedThisStage = false;
            method.Value.ThisStageMethodId = null;
        }
    }

    /// <summary>
    /// Removes unreferenced code including methods, variables, resources, cbuffers, and stream types.
    /// Preserves logical groups and resource groups where at least one member is used.
    /// </summary>
    public static void RemoveUnreferencedCode(NewSpirvBuffer buffer, SpirvContext context, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
    {
        // Remove unreferenced code
        var removedIds = new HashSet<int>();

        // First, build resource group (used status and logical groups)
        var logicalGroups = new Dictionary<string, LogicalGroupInfo>();
        foreach (var resourceGroup in analysisResult.ResourceGroups)
        {
            foreach (var resource in resourceGroup.Value.Resources)
            {
                if (resource.UsedAnyStage)
                {
                    resourceGroup.Value.Used = true;
                }

                if (resourceGroup.Value.LogicalGroup != null)
                {
                    ref var logicalGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(logicalGroups, $"{resourceGroup.Value.Name}.{resourceGroup.Value.LogicalGroup}", out var exists);
                    if (!exists)
                        logicalGroup = new();
                    logicalGroup.Resources.Add(resourceGroup.Value);
                }
            }
        }
        // Complete logical groups with cbuffers
        foreach (var cbuffer in analysisResult.CBuffers)
        {
            if (cbuffer.Value.LogicalGroup != null)
            {
                ref var logicalGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(logicalGroups, $"{cbuffer.Value.Name}.{cbuffer.Value.LogicalGroup}", out var exists);
                if (!exists)
                    logicalGroup = new();
                logicalGroup.CBuffers.Add(cbuffer.Value);
            }
        }

        // Check logical group: if any resource is used, mark everything as used
        // TODO: make sure register allocation is contiguous
        foreach (var logicalGroup in logicalGroups)
        {
            var logicalGroupUsed = false;
            foreach (var resource in logicalGroup.Value.Resources)
                logicalGroupUsed |= resource.Used;
            foreach (var cbuffer in logicalGroup.Value.CBuffers)
                logicalGroupUsed |= cbuffer.UsedAnyStage;

            if (logicalGroupUsed)
            {
                // Mark everything as used
                foreach (var resource in logicalGroup.Value.Resources)
                    resource.Used = logicalGroupUsed;
                foreach (var cbuffer in logicalGroup.Value.CBuffers)
                    cbuffer.UsedThisStage = logicalGroupUsed;
            }
        }

        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index];
            if (i.Op == Op.OpFunction && (OpFunction)i is { } function)
            {
                bool isReferenced = liveAnalysis.ReferencedMethods.ContainsKey(function.ResultId)
                    || liveAnalysis.ExtraReferencedMethods.Contains(function.ResultId);
                if (!isReferenced)
                {
                    removedIds.Add(function.ResultId);
                    while (buffer[index].Op != Op.OpFunctionEnd)
                    {
                        if (buffer[index].Data.IdResult is int resultId)
                            removedIds.Add(resultId);
                        SpirvBuilder.SetOpNop(buffer[index++].Data.Memory.Span);
                    }

                    SpirvBuilder.SetOpNop(buffer[index].Data.Memory.Span);
                }
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    Storageclass: StorageClass.Uniform,
                    ResultId: int
                } variable
                && analysisResult.CBuffers.TryGetValue(variable, out var cbufferInfo))
            {
                if (!cbufferInfo.UsedAnyStage)
                {
                    removedIds.Add(variable.ResultId);
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    Storageclass: StorageClass.UniformConstant,
                    ResultId: int
                } resource)
            {
                var resourceInfo = analysisResult.Resources[resource.ResultId];
                // If resource has a rgroup, check its state (if any resource is used in the group, we need to keep every resource)
                // If no rgroup, we check the resource itself
                if (!(resourceInfo.ResourceGroup?.Used ?? false || resourceInfo.UsedAnyStage))
                {
                    removedIds.Add(resource.ResultId);
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
            }

            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is
                {
                    Storageclass: StorageClass.Private or StorageClass.Workgroup,
                    ResultId: int
                } variable2)
            {
                if (variable2.Flags.HasFlag(VariableFlagsMask.Stream))
                {
                    // Always removed as we now use streams structure
                    removedIds.Add(variable2.ResultId);
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
                else
                {
                    if (!analysisResult.Variables.TryGetValue(variable2.ResultId, out var variableInfo) || !variableInfo.UsedAnyStage)
                    {
                        removedIds.Add(variable2.ResultId);
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    }
                }
            }
        }

        // Remove all OpTypeStreamsSDSL, OpTypePatchSDSL and OpTypeGeometryStreamOutputSDSL or any type that depends on it
        // (we do that before the OpName/OpDecorate pass)
        foreach (var i in context)
        {
            if (i.Op == Op.OpTypeStreamsSDSL || i.Op == Op.OpTypeGeometryStreamOutputSDSL || i.Op == Op.OpTypePatchSDSL || i.Op == Op.OpTypeFunctionSDSL || i.Op == Op.OpTypePointer || i.Op == Op.OpTypeArray)
            {
                if (context.ReverseTypes.TryGetValue(i.Data.IdResult.Value, out var type))
                {
                    var streamsTypeSearch = new ReadWriteAnalyzer.StreamsTypeSearch();
                    streamsTypeSearch.VisitType(type);
                    if (streamsTypeSearch.Found)
                    {
                        removedIds.Add(i.Data.IdResult.Value);
                        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                    }
                }
            }
        }

        // Remove OpName/OpDecorate
        context.RemoveNameAndDecorations(removedIds);
    }
}
