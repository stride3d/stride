using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Transformation;

/// <summary>
/// Handles duplication of methods for different shader stages.
/// </summary>
internal static class MethodDuplicator
{

    /// <summary>
    /// Duplicates a method if it's used by multiple shader stages with different STREAMS types.
    /// On first use, backs up the original method code. On subsequent uses, creates a copy with new IDs.
    /// </summary>
    public static void DuplicateMethodIfNecessary(
        NewSpirvBuffer buffer,
        SpirvContext context,
        int functionId,
        AnalysisResult analysisResult,
        LiveAnalysis liveAnalysis,
        Action<int, int>? codeInserted = null)
    {
        var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);

        (var methodStart, var methodEnd) = SpirvBuilder.FindMethodBounds(buffer, functionId);

        // One function might need to be duplicated in case it is used by different shader stages with STREAMS:
        // On first time (in a stage), we backup method original content before mutation
        // On second time (in a different stage), we copy the method (from original content)
        if (methodInfo.OriginalMethodCode == null)
        {
            // Copy instructions memory (since we're going to mutate them and want to retain original version)
            var methodInstructions = buffer.Slice(methodStart, methodEnd - methodStart);
            foreach (ref var i in CollectionsMarshal.AsSpan(methodInstructions))
                i = new OpData(i.Memory.Span);

            methodInfo.OriginalMethodCode = methodInstructions;
        }
        else
        {
            // Need to reinsert method with new IDs
            var remapIds = new Dictionary<int, int>();
            var copiedInstructions = new List<OpData>();
            foreach (var i in methodInfo.OriginalMethodCode)
            {
                // Save copied function ID
                if (i.Op == Op.OpFunction)
                    methodInfo.ThisStageMethodId = context.Bound;

                var i2 = new OpData(i.Memory.Span);
                if (i2.IdResult.HasValue)
                    remapIds.Add(i2.IdResult.Value, context.Bound++);
                SpirvBuilder.RemapIds(remapIds, ref i2);
                copiedInstructions.Add(i2);

                // Copy names too
                if (i.IdResult is int resultId)
                {
                    if (analysisResult.Names.TryGetValue(resultId, out var name))
                        context.AddName(i2.IdResult!.Value, name);
                }
            }

            if (methodInfo.ThisStageMethodId == null)
                throw new InvalidOperationException();

            liveAnalysis.ExtraReferencedMethods.Add(methodInfo.ThisStageMethodId.Value);

            // TODO: adjust mixin instructions ranges
            buffer.InsertRange(methodEnd, CollectionsMarshal.AsSpan(copiedInstructions));
            codeInserted?.Invoke(methodEnd, copiedInstructions.Count);
        }
    }
}
