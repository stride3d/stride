using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces.Models;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing.Interfaces.Transformation;

/// <summary>
/// Handles patching of SPIR-V stream access instructions.
/// </summary>
internal static class StreamAccessPatcher
{
    /// <summary>
    /// Type rewriter that replaces StreamsType with concrete struct types.
    /// </summary>
    private class StreamsTypeReplace(SymbolType streamsReplacement, SymbolType inputReplacement, SymbolType outputReplacement, SymbolType? constantsReplacement) : TypeRewriter
    {
        public override SymbolType Visit(StreamsType streamsType)
        {
            return streamsType.Kind switch
            {
                StreamsKindSDSL.Streams => streamsReplacement,
                StreamsKindSDSL.Input => inputReplacement,
                StreamsKindSDSL.Output => outputReplacement,
                StreamsKindSDSL.Constants => constantsReplacement ?? throw new InvalidOperationException(),
            };
        }
    }

    /// <summary>
    /// Patches stream accesses in a method to use the concrete struct types instead of abstract stream types.
    /// </summary>
    public static void PatchStreamsAccesses(
        SymbolTable table,
        NewSpirvBuffer buffer,
        SpirvContext context,
        int functionId,
        StructType streamsStructType,
        StructType inputStructType,
        StructType outputStructType,
        StructType? constantsStructType,
        int streamsVariableId,
        AnalysisResult analysisResult,
        LiveAnalysis liveAnalysis)
    {
        var methodInfo = liveAnalysis.GetOrCreateMethodInfo(functionId);

        (var methodStart, _) = SpirvBuilder.FindMethodBounds(buffer, methodInfo.ThisStageMethodId ?? functionId);

        var streams = analysisResult.Streams;
        // true => implicit (streams.), false => specific variable
        var streamsInstructionIds = new Dictionary<int, bool>();

        var method = (OpFunction)buffer[methodStart];
        var methodType = (FunctionType)context.ReverseTypes[method.FunctionType];

        var streamTypeReplacer = new StreamsTypeReplace(streamsStructType, inputStructType, outputStructType, constantsStructType);
        var newMethodType = (FunctionType)streamTypeReplacer.Visit(methodType);
        if (!ReferenceEquals(newMethodType, methodType))
        {
            methodType = newMethodType;
            method.FunctionType = context.GetOrRegister(methodType);
            var symbol = table.ResolveSymbol(functionId);
            symbol.Type = methodType;
        }

        // Remap ids for streams type to actual struct type
        var remapIds = new Dictionary<int, int>();
        var processedIds = new HashSet<int>();

        // Check if type contains any Streams/Input/Output (and if yes, register the replacement)
        void CheckStreamTypes(int id)
        {
            if (processedIds.Add(id) && context.ReverseTypes.TryGetValue(id, out var type))
            {
                // New type, check it
                var replacedType = streamTypeReplacer.VisitType(type);
                if (!ReferenceEquals(replacedType, type))
                    remapIds.Add(id, context.GetOrRegister(replacedType));
            }
        }

        // TODO: remap method type!
        Span<int> tempIdsForStreamCopy = stackalloc int[streams.Values.Count];
        for (int index = methodStart; ; ++index)
        {
            var i = buffer[index];

            if (i.Op == Op.OpFunctionEnd)
                break;

            if (i.Op == Op.OpStreamsSDSL && (OpStreamsSDSL)i is { } streamsInstruction)
            {
                streamsInstructionIds.Add(streamsInstruction.ResultId, true);
                remapIds.Add(streamsInstruction.ResultId, streamsVariableId);
                SpirvBuilder.SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Op is Op.OpVariable && (OpVariable)i is { } variable)
            {
                var type = context.ReverseTypes[variable.ResultType];
                if (type is PointerType { BaseType: StreamsType })
                    streamsInstructionIds.Add(variable.ResultId, false);
            }
            else if (i.Op is Op.OpFunctionParameter && (OpFunctionParameter)i is { } functionParameter)
            {
                var type = context.ReverseTypes[functionParameter.ResultType];
                if (type is PointerType { BaseType: StreamsType })
                    streamsInstructionIds.Add(functionParameter.ResultId, false);
            }
            else if (i.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain)
            {
                // In case it's a streams access, patch acces to use STREAMS struct with proper index
                if (streamsInstructionIds.TryGetValue(accessChain.BaseId, out var isImplicit))
                {
                    var streamVariableId = accessChain.Values.Elements.Span[0];
                    var streamInfo = streams[streamVariableId];
                    var streamStructMemberIndex = streamInfo.StreamStructFieldIndex;

                    // TODO: this won't update accessChain.Memory yet but setting accessChain.Base later will fix that
                    //       we'll need a better way to update LiteralArray and propagate changes
                    accessChain.Values.Elements.Span[0] = context.CompileConstant(streamStructMemberIndex).Id;

                    if (isImplicit)
                        accessChain.BaseId = streamsVariableId;
                    else
                        // Force refresh of InstructionMemory
                        // TODO: remove when accessChain.Values update properly the instruction
                        accessChain.BaseId = accessChain.BaseId;
                }
            }
            else if (i.Op == Op.OpCopyLogical && (OpCopyLogical)i is { } copyLogical)
            {
                // Cast input to streams
                var targetType = context.ReverseTypes[copyLogical.ResultType];
                if (targetType is StreamsType { Kind: StreamsKindSDSL.Streams })
                {
                    foreach (var stream in streams)
                    {
                        // Part of streams?
                        if (!stream.Value.Patch && stream.Value.UsedThisStage)
                        {
                            if (stream.Value.Input)
                            {
                                // Extract value from streams
                                tempIdsForStreamCopy[stream.Value.StreamStructFieldIndex] = buffer.Insert(index++,
                                    new OpCompositeExtract(context.GetOrRegister(stream.Value.Type),
                                        context.Bound++,
                                        copyLogical.Operand,
                                        [stream.Value.InputStructFieldIndex.Value])).ResultId;
                            }
                            else
                            {
                                // Otherwise use default value
                                tempIdsForStreamCopy[stream.Value.StreamStructFieldIndex] = context.CreateDefaultConstantComposite(stream.Value.Type).Id;
                            }
                        }
                    }

                    // Update index (otherwise copyLogical fields will point to invalid data)
                    i.Index = index;
                    buffer.Replace(index, new OpCompositeConstruct(copyLogical.ResultType, copyLogical.ResultId, [.. tempIdsForStreamCopy.Slice(0, streamsStructType.Members.Count)]));
                }
                else if (targetType is StreamsType { Kind: StreamsKindSDSL.Output })
                {
                    foreach (var stream in streams)
                    {
                        // Part of streams?
                        if (!stream.Value.Patch && stream.Value.Output)
                        {
                            // Extract value from streams
                            tempIdsForStreamCopy[stream.Value.OutputStructFieldIndex.Value] = buffer.Insert(index++,
                                new OpCompositeExtract(context.GetOrRegister(stream.Value.Type),
                                    context.Bound++,
                                    copyLogical.Operand,
                                    [stream.Value.StreamStructFieldIndex])).ResultId;
                        }
                    }

                    // Update index (otherwise copyLogical fields will point to invalid data)
                    i.Index = index;
                    buffer.Replace(index, new OpCompositeConstruct(copyLogical.ResultType, copyLogical.ResultId, [.. tempIdsForStreamCopy.Slice(0, outputStructType.Members.Count)]));
                }
            }
            else if (i.Op == Op.OpEmitVertexSDSL && (OpEmitVertexSDSL)i is { } emitVertex)
            {
                var output = emitVertex.Output;
                foreach (var stream in streams)
                {
                    if (stream.Value.Output)
                    {
                        var outputValue = buffer.Insert(index++, new OpCompositeExtract(context.GetOrRegister(stream.Value.Type), context.Bound++, output, [stream.Value.OutputStructFieldIndex.Value])).ResultId;
                        buffer.Insert(index++, new OpStore(stream.Value.OutputId.Value, outputValue, MemoryAccessMask.None, []));
                    }
                }

                buffer.Replace(index, new OpEmitVertex());
            }
            else if (i.Op == Op.OpFunctionCall && (OpFunctionCall)i is { } call)
            {
                var calledMethodInfo = liveAnalysis.ReferencedMethods[call.Function];
                // In case we copied the method, use the new ID
                if (calledMethodInfo.ThisStageMethodId is int updatedMethodId)
                    call.Function = updatedMethodId;
            }

            SpirvBuilder.CollectIds(i.Data, CheckStreamTypes);

            SpirvBuilder.RemapIds(remapIds, ref i.Data);
        }
    }
}
