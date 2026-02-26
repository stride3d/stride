using Stride.Shaders.Core;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Analysis;

internal static class ReadWriteAnalyzer
{
    /// <summary>
    /// Figure out (recursively) which streams are being read from and written to.
    /// </summary>
    public static bool AnalyzeStreamReadWrites(SpirvBuffer buffer, SpirvContext context, int functionId, AnalysisResult analysisResult, LiveAnalysis liveAnalysis)
    {
        // Check if already processed
        var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);
        if (methodInfo.UsedThisStage)
        {
            return methodInfo.HasStreamAccess;
        }

        // Mark as used
        methodInfo.UsedThisStage = true;

        // If method was mutated by another stage, we work on the original copy instead
        List<OpData> methodInstructions;
        if (methodInfo.OriginalMethodCode != null)
        {
            methodInstructions = methodInfo.OriginalMethodCode;
        }
        else
        {
            (var methodStart, var methodEnd) = SpirvBuilder.FindMethodBounds(buffer, functionId);
            methodInstructions = buffer.Slice(methodStart, methodEnd - methodStart);
        }

        var streamsInstructionIds = new Dictionary<int, StreamsKindSDSL>();
        var patchInstructionIds = new Dictionary<int, StreamsKindSDSL>();
        var streams = analysisResult.Streams;
        var variables = analysisResult.Variables;
        var accessChainBases = new Dictionary<int, (int Base, StreamsKindSDSL? StreamKind)>();

        foreach (ref var i in CollectionsMarshal.AsSpan(methodInstructions))
        {
            // Check for any Streams variable
            if (i.Op is Op.OpFunction && new OpFunction(ref i) is { } function)
            {
                var functionType = (FunctionType)context.ReverseTypes[function.FunctionType];
                var streamsTypeSearch = new StreamsTypeSearch();
                streamsTypeSearch.VisitType(functionType);
                if (streamsTypeSearch.Found)
                    methodInfo.HasStreamAccess = true;
            }
            else if (i.Op is Op.OpVariable && new OpVariable(ref i) is { } variable)
            {
                var type = context.ReverseTypes[variable.ResultType];
                if (type is PointerType { BaseType: StreamsType s })
                {
                    // Note: we should restrict to R except if inout variable
                    streamsInstructionIds.Add(variable.ResultId, s.Kind);
                    methodInfo.HasStreamAccess = true;
                }
            }
            // and for any Streams parameter
            else if (i.Op is Op.OpFunctionParameter && new OpFunctionParameter(ref i) is { } functionParameter)
            {
                var type = context.ReverseTypes[functionParameter.ResultType];
                if (type is PointerType { BaseType: StreamsType s })
                {
                    // Note: we should restrict to R except if inout variable
                    streamsInstructionIds.Add(functionParameter.ResultId, s.Kind);
                    methodInfo.HasStreamAccess = true;
                }
                else if (type is PointerType { BaseType: PatchType { BaseType: StreamsType s2 } })
                {
                    patchInstructionIds.Add(functionParameter.ResultId, s2.Kind);
                    methodInfo.HasStreamAccess = true;
                }
            }
            else if (i.Op is Op.OpLoad && new OpLoad(ref i) is { } load)
            {
                // Check for indirect access chains
                if (!accessChainBases.TryGetValue(load.Pointer, out var accessChain))
                    accessChain.Base = load.Pointer;
                if (streams.TryGetValue(accessChain.Base, out var streamInfo))
                {
                    var streamKind = accessChain.StreamKind.Value;

                    // If read on input/output stream, we force it to be emitted in the input/output struct
                    if (streamKind == StreamsKindSDSL.Output)
                    {
                        if (!streamInfo.Output)
                            streamInfo.InternalPatchConstantOutput = true;
                        streamInfo.Output = true;
                    }
                    else if (streamKind == StreamsKindSDSL.Input)
                        streamInfo.Read = true;
                    else
                    {
                        // In case of streams or constants access, check if there was a previous write on the variable
                        // (in which case it is read after being written, which we do not need to write as a read from previous stage)
                        if (!streamInfo.Write)
                            streamInfo.Read = true;
                    }
                }
                if (variables.TryGetValue(accessChain.Base, out var variableInfo))
                    variableInfo.UsedThisStage = true;
                if (analysisResult.Resources.TryGetValue(accessChain.Base, out var resourceInfo))
                    resourceInfo.UsedThisStage = true;
                if (analysisResult.CBuffers.TryGetValue(accessChain.Base, out var cbufferInfo))
                    cbufferInfo.UsedThisStage = true;
            }
            else if (i.Op is Op.OpStore && new OpStore(ref i) is { } store)
            {
                // Check for indirect access chains
                if (!accessChainBases.TryGetValue(store.Pointer, out var accessChain))
                    accessChain.Base = store.Pointer;

                if (streams.TryGetValue(accessChain.Base, out var streamInfo))
                {
                    var streamKind = accessChain.StreamKind.Value;
                    // Write on input/output stream are not allowed
                    if (streamKind is StreamsKindSDSL.Input or StreamsKindSDSL.Output)
                        throw new InvalidOperationException("Can't write value on input or output struct");

                    streamInfo.Write = true;
                }
                if (variables.TryGetValue(accessChain.Base, out var variableInfo))
                    variableInfo.UsedThisStage = true;
                if (analysisResult.Resources.TryGetValue(accessChain.Base, out var resourceInfo))
                    resourceInfo.UsedThisStage = true;
                if (analysisResult.CBuffers.TryGetValue(accessChain.Base, out var cbufferInfo))
                    cbufferInfo.UsedThisStage = true;
            }
            else if (i.Op == Op.OpStreamsSDSL && new OpStreamsSDSL(ref i) is { } streamsInstruction)
            {
                streamsInstructionIds.Add(streamsInstruction.ResultId, StreamsKindSDSL.Streams);
                methodInfo.HasStreamAccess = true;
            }
            else if (i.Op == Op.OpAccessChain && new OpAccessChain(ref i) is { } accessChain)
            {
                var currentBase = accessChain.BaseId;

                // In case it's a patch access, i.e. patch[0], mark the access as being a stream
                if (patchInstructionIds.TryGetValue(currentBase, out var patchStreamKind))
                {
                    var patchVariableId = accessChain.Values.Elements.Span[0];
                    if (accessChain.Values.Elements.Length > 1)
                        throw new InvalidOperationException("OpAccessChain on PatchType can have only 1 element");
                    streamsInstructionIds.Add(accessChain.ResultId, patchStreamKind);
                }
                else
                {
                    StreamsKindSDSL? currentStreamKind = null;
                    // In case it's a streams access, mark the stream as being the base
                    if (streamsInstructionIds.TryGetValue(currentBase, out var streamKind))
                    {
                        var streamVariableId = accessChain.Values.Elements.Span[0];
                        var streamInfo = streams[streamVariableId];

                        // Set this base for OpStore/OpLoad stream R/W analysis
                        currentBase = streamVariableId;
                        currentStreamKind = streamKind;
                    }

                    // Any read or write through an access chain will be treated as doing it on the main variable.
                    // i.e., streams.A.B will share same streamInfo as streams.A
                    // TODO: what happens in case of partial write?
                    // Recurse in case we have multiple access chain chained after each other
                    while (accessChainBases.TryGetValue(currentBase, out var nextBase))
                    {
                        currentBase = nextBase.Base;
                        currentStreamKind ??= nextBase.StreamKind;
                    }
                    accessChainBases.Add(accessChain.ResultId, (currentBase, currentStreamKind));
                }
            }
            else if (i.Op == Op.OpFunctionCall && new OpFunctionCall(ref i) is { } call)
            {
                // Process call
                methodInfo.HasStreamAccess |= AnalyzeStreamReadWrites(buffer, context, call.Function, analysisResult, liveAnalysis);
            }
        }

        return methodInfo.HasStreamAccess;
    }

    internal class StreamsTypeSearch : TypeWalker
    {
        public bool Found { get; private set; }
        public override void VisitStreamsType(StreamsType streamsType)
        {
            Found = true;
        }
        public override void VisitGeometryStreamType(GeometryStreamType geometryStreamsType)
        {
            Found = true;
        }

        public override void VisitPatchType(PatchType patchType)
        {
            Found = true;
        }
    }
}
