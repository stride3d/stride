using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Cleanup;

/// <summary>
/// Handles merging of variables with the same semantic and propagating streams between shader stages.
/// </summary>
internal static class VariableMerger
{
    /// <summary>
    /// Merges variables that have the same semantic into a single variable.
    /// </summary>
    public static void MergeSameSemanticVariables(SymbolTable table, SpirvContext context, NewSpirvBuffer buffer, AnalysisResult analysisResult)
    {
        Dictionary<int, int> remapIds = new();
        foreach (var streamWithSameSemantic in analysisResult.Streams.Where(x => x.Value.Semantic != null).GroupBy(x => x.Value.Semantic))
        {
            // Make sure they all have the same type
            var firstStream = streamWithSameSemantic.First();
            foreach (var stream in streamWithSameSemantic.Skip(1))
            {
                if (stream.Value.Type != firstStream.Value.Type)
                    throw new InvalidOperationException($"Two variables with same semantic {stream.Value.Semantic} have different types {stream.Value.Type} and {firstStream.Value.Type}");

                // Remap variable
                remapIds.Add(stream.Key, firstStream.Key);
            }
        }

        // Remove duplicate streams
        HashSet<int> removedIds = new();
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is { } variable && remapIds.ContainsKey(variable.ResultId))
            {
                SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                removedIds.Add(variable.ResultId);
            }
        }

        // Remove OpName/OpDecorate
        context.RemoveNameAndDecorations(removedIds);

        foreach (var remapId in remapIds)
            analysisResult.Streams.Remove(remapId.Key);

        SpirvBuilder.RemapIds(buffer, 0, buffer.Count, remapIds);
    }

    /// <summary>
    /// Propagates stream variables from the previous shader stage by converting inputs to outputs.
    /// </summary>
    public static void PropagateStreamsFromPreviousStage(Dictionary<int, StreamVariableInfo> streams)
    {
        foreach (var stream in streams)
        {
            stream.Value.OutputLayoutLocation = stream.Value.InputLayoutLocation;
            stream.Value.InputLayoutLocation = null;
            stream.Value.Output = stream.Value.Input;
            stream.Value.Read = false;
            stream.Value.Write = false;
        }
    }
}
