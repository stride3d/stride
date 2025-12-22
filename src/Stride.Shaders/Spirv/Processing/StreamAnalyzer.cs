using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System.IO;
using Stride.Shaders.Parsing.Analysis;
using static Stride.Shaders.Spirv.Specification;
using System.Runtime.InteropServices;

namespace Stride.Shaders.Spirv.Processing
{
    public class StreamAnalyzer
    {
        class StreamInfo(string? semantic, string name, SymbolType type, int variableId)
        {
            public string? Semantic { get; } = semantic;
            public string Name { get; } = name;
            public SymbolType Type { get; } = type;

            public int VariableId { get; } = variableId;
            public int? VariableMethodInitializerId { get; set; }

            public int? InputLayoutLocation { get; set; }
            public int? OutputLayoutLocation { get; set; }

            /// <summary>
            /// We automatically mark input: a variable read before it's written to, or an output without a write.
            /// </summary>
            public bool Input => Read || (Output && !Write);
            public bool Output { get; set; }
            public bool Private => Input || Output || Read || Write;

            public bool Read { get; set; }
            public bool Write { get; set; }

            public override string ToString() => $"{Type} {Name} {(Read ? "R" : "")} {(Write ? "W" : "")}";
        }

        class ResourceInfo
        {
            public bool Used { get; set; }
        }

        record struct AnalysisResult(SortedList<int, StreamInfo> Streams, List<int> Blocks, SortedList<int, ResourceInfo> Resources);

        public void Process(SymbolTable table, NewSpirvBuffer buffer, SpirvContext context)
        {
            table.TryResolveSymbol("VSMain", out var entryPointVS);
            var entryPointPS = table.ResolveSymbol("PSMain");

            if (entryPointVS.Type is FunctionGroupType)
                entryPointVS = entryPointVS.GroupMembers[^1];
            if (entryPointPS.Type is FunctionGroupType)
                entryPointPS = entryPointPS.GroupMembers[^1];

            if (entryPointPS.IdRef == 0)
                throw new InvalidOperationException($"{nameof(StreamAnalyzer)}: At least a pixel shader is expected");

            var analysisResult = Analyze(buffer, context);
            MergeSameSemanticVariables(table, context, buffer, analysisResult);
            var streams = analysisResult.Streams;

            AnalyzeStreamReadWrites(buffer, [], entryPointPS.IdRef, analysisResult);

            // If written to, they are expected at the end of pixel shader
            foreach (var stream in streams)
            {
                if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant().StartsWith("SV_TARGET") || semantic.ToUpperInvariant() == "SV_DEPTH")
                    && stream.Value.Write)
                    stream.Value.Output = true;
            }

            var psWrapper = GenerateStreamWrapper(buffer, context, ExecutionModel.Fragment, entryPointPS.IdRef, entryPointPS.Id.Name, analysisResult);

            // Those semantic variables are implicit in pixel shader, no need to forward them from previous stages
            foreach (var stream in streams)
            {
                if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant() == "SV_COVERAGE" || semantic.ToUpperInvariant() == "SV_ISFRONTFACE" || semantic.ToUpperInvariant() == "VFACE"))
                    stream.Value.Read = false;
            }
            PropagateStreamsFromPreviousStage(streams);
            if (entryPointVS.IdRef != 0)
            {
                AnalyzeStreamReadWrites(buffer, [], entryPointVS.IdRef, analysisResult);

                // Expected at the end of vertex shader
                foreach (var stream in streams)
                {
                    // If written to, they are expected at the end of pixel shader
                    if (stream.Value.Semantic is { } semantic && (semantic.ToUpperInvariant().StartsWith("SV_POSITION"))
                        && stream.Value.Write)
                        stream.Value.Output = true;
                }

                GenerateStreamWrapper(buffer, context, ExecutionModel.Vertex, entryPointVS.IdRef, entryPointVS.Id.Name, analysisResult);
            }

            buffer.FluentAdd(new OpExecutionMode(psWrapper.ResultId, ExecutionMode.OriginUpperLeft));
        }

        private void MergeSameSemanticVariables(SymbolTable table, SpirvContext context, NewSpirvBuffer buffer, AnalysisResult analysisResult)
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
            foreach (var i in buffer)
            {
                if (i.Op == Op.OpVariableSDSL && ((OpVariableSDSL)i) is { } variable && remapIds.ContainsKey(variable.ResultId))
                {
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
            }

            foreach (var remapId in remapIds)
                analysisResult.Streams.Remove(remapId.Key);

            SpirvBuilder.RemapIds(buffer, 0, buffer.Count, remapIds);
        }

        private static void PropagateStreamsFromPreviousStage(SortedList<int, StreamInfo> streams)
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

        private AnalysisResult Analyze(NewSpirvBuffer buffer, SpirvContext context)
        {
            var streams = new SortedList<int, StreamInfo>();

            HashSet<int> blockTypes = [];
            Dictionary<int, int> blockPointerTypes = [];
            List<int> blockIds = [];
            SortedList<int, ResourceInfo> resources = [];

            // Build name table
            SortedList<int, string> nameTable = [];
            SortedList<int, string> semanticTable = [];
            foreach (var temp in new[] { context.GetBuffer(), buffer })
            {
                foreach (var instruction in temp)
                {
                    // Names
                    {
                        if (instruction.Op == Op.OpName
                            && ((OpName)instruction) is
                            {
                                Target: int t,
                                Name: string n
                            }
                           )
                        {
                            nameTable[t] = new(n);
                        }
                        else if (instruction.Op == Op.OpMemberName
                            && ((OpMemberName)instruction) is
                            {
                                Type: int t2,
                                Member: int m,
                                Name: string n2
                            }
                           )
                        {
                            nameTable[t2] = new(n2);
                        }
                    }

                    // CBuffer
                    // Encoded in this format:
                    // OpDecorate %type_CBuffer1 Block
                    // %_ptr_Uniform_type_CBuffer1 = OpTypePointer Uniform %type_CBuffer1
                    // %CBuffer1 = OpVariable %_ptr_Uniform_type_CBuffer1 Uniform
                    {
                        if (instruction.Op == Op.OpDecorate
                            && ((OpDecorate)instruction) is { Decoration: { Value: Decoration.Block }, Target: var bufferType })
                        {
                            blockTypes.Add(bufferType);
                        }
                        else if (instruction.Op == Op.OpTypePointer
                            && ((OpTypePointer)instruction) is { Storageclass: StorageClass.Uniform, ResultId: var pointerType, Type: var bufferType2 }
                            && blockTypes.Contains(bufferType2))
                        {
                            blockPointerTypes.Add(pointerType, bufferType2);
                        }
                        else if (instruction.Op == Op.OpVariableSDSL
                            && ((OpVariableSDSL)instruction) is { Storageclass: StorageClass.Uniform, ResultType: var pointerType2, ResultId: var bufferId }
                            && blockPointerTypes.TryGetValue(pointerType2, out var bufferType3))
                        {
                            blockIds.Add(bufferId);
                        }
                    }

                    // Semantic
                    {
                        if (instruction.Op == Op.OpDecorateString
                            && ((OpDecorateString)instruction) is
                            {
                                Target: int t,
                                Decoration:
                                {
                                    Value: Decoration.UserSemantic,
                                    Parameters: { } m
                                }
                            }
                           )
                        {
                            using var n = new LiteralValue<string>(m.Span);
                            semanticTable[t] = n.Value;
                        }
                    }
                }
            }

            // Analyze streams
            foreach (var temp in new[] { context.GetBuffer(), buffer })
            {
                foreach (var instruction in temp)
                {
                    if (instruction.Op == Op.OpVariableSDSL && ((OpVariableSDSL)instruction) is
                        {
                            Storageclass: StorageClass.Private,
                            ResultId: int
                        } variable)
                    {
                        var name = nameTable.TryGetValue(variable.ResultId, out var nameId)
                            ? nameId
                            : $"unnamed_{variable.ResultId}";
                        var type = context.ReverseTypes[variable.ResultType];
                        semanticTable.TryGetValue(variable.ResultId, out var semantic);

                        var stream = new StreamInfo(semantic, name, type, variable.ResultId)
                        {
                            // Does it have an initializer? if yes, mark it as a value written in this stage
                            Write = variable.MethodInitializer != null,
                            VariableMethodInitializerId = variable.MethodInitializer,
                        };

                        streams.Add(variable.ResultId, stream);
                    }

                    if (instruction.Op == Op.OpVariableSDSL && ((OpVariableSDSL)instruction) is
                        {
                            Storageclass: StorageClass.UniformConstant,
                            ResultId: int
                        } resource)
                    {
                        var name = nameTable.TryGetValue(resource.ResultId, out var nameId)
                            ? nameId
                            : $"unnamed_{resource.ResultId}";
                        var type = context.ReverseTypes[resource.ResultType];

                        resources.Add(resource.ResultId, new ResourceInfo());
                    }
                }
            }

            return new(streams, blockIds, resources);
        }

        private OpFunction GenerateStreamWrapper(NewSpirvBuffer buffer, SpirvContext context, ExecutionModel executionModel, int entryPointId, string entryPointName, AnalysisResult analysisResult)
        {
            var streams = analysisResult.Streams;

            var stage = executionModel switch
            {
                ExecutionModel.Fragment => "PS",
                ExecutionModel.Vertex => "VS",
                _ => throw new NotImplementedException()
            };
            List<(StreamInfo Info, int Id)> inputStreams = [];
            List<(StreamInfo Info, int Id)> outputStreams = [];
            List<StreamInfo> privateStreams = [];

            int inputLayoutLocationCount = 0;
            int outputLayoutLocationCount = 0;

            foreach (var stream in streams)
            {
                if (stream.Value.Output)
                {
                    if (stream.Value.OutputLayoutLocation is { } outputLayoutLocation)
                    {
                        outputLayoutLocationCount = Math.Max(outputLayoutLocation + 1, outputLayoutLocationCount);
                    }
                }
            }

            foreach (var stream in streams)
            {
                var baseType = ((PointerType)stream.Value.Type).BaseType;
                if (stream.Value.Private)
                    privateStreams.Add(stream.Value);

                if (stream.Value.Input)
                {
                    context.FluentAdd(new OpTypePointer(context.Bound++, StorageClass.Input, context.Types[baseType]), out var pointerType);
                    context.FluentAdd(new OpVariable(pointerType, context.Bound++, StorageClass.Input, null), out var variable);
                    context.AddName(variable, $"in_{stage}_{stream.Value.Name}");

                    switch (stream.Value.Semantic?.ToUpperInvariant())
                    {
                        case "SV_ISFRONTFACE":
                            context.Add(new OpDecorate(variable, ParameterizedFlags.DecorationBuiltIn(BuiltIn.FrontFacing)));
                            context.Add(new OpDecorate(variable, Decoration.Flat));
                            break;
                        default:
                            if (stream.Value.InputLayoutLocation == null)
                                stream.Value.InputLayoutLocation = inputLayoutLocationCount++;
                            context.Add(new OpDecorate(variable, ParameterizedFlags.DecorationLocation(stream.Value.InputLayoutLocation.Value)));
                            if (stream.Value.Semantic != null)
                                context.Add(new OpDecorateString(variable, ParameterizedFlags.DecorationUserSemantic(stream.Value.Semantic)));
                            break;
                    }

                    inputStreams.Add((stream.Value, variable.ResultId));
                }

                if (stream.Value.Output)
                {
                    context.FluentAdd(new OpTypePointer(context.Bound++, StorageClass.Output, context.Types[baseType]), out var pointerType);
                    context.FluentAdd(new OpVariable(pointerType, context.Bound++, StorageClass.Output, null), out var variable);
                    context.AddName(variable, $"out_{stage}_{stream.Value.Name}");

                    switch (stream.Value.Semantic?.ToUpperInvariant())
                    {
                        case "SV_POSITION":
                            context.Add(new OpDecorate(variable, ParameterizedFlags.DecorationBuiltIn(BuiltIn.Position)));
                            break;
                        default:
                            // TODO: this shouldn't be necessary if we allocated layout during first forward pass for any SV_ semantic
                            if (stream.Value.OutputLayoutLocation == null)
                            {
                                if (stream.Value.Semantic?.ToUpperInvariant().StartsWith("SV_") ?? false)
                                    stream.Value.OutputLayoutLocation = outputLayoutLocationCount++;
                                else
                                    throw new InvalidOperationException($"Can't find output layout location for variable [{stream.Value.Name}]");
                            }

                            context.Add(new OpDecorate(variable, ParameterizedFlags.DecorationLocation(stream.Value.OutputLayoutLocation.Value)));
                            if (stream.Value.Semantic != null)
                                context.Add(new OpDecorateString(variable, ParameterizedFlags.DecorationUserSemantic(stream.Value.Semantic)));
                            break;
                    }

                    outputStreams.Add((stream.Value, variable.ResultId));
                }
            }

            context.FluentAdd(new OpTypeVoid(context.Bound++), out var voidType);

            // Add new entry point wrapper
            context.FluentAdd(new OpTypeFunctionSDSL(context.Bound++, voidType, []), out var newEntryPointFunctionType);
            buffer.FluentAdd(new OpFunction(voidType, context.Bound++, FunctionControlMask.None, newEntryPointFunctionType), out var newEntryPointFunction);
            buffer.Add(new OpLabel(context.Bound++));
            context.AddName(newEntryPointFunction, $"{entryPointName}_Wrapper");

            {
                // Variable initializers
                foreach (var stream in streams)
                {
                    // Note: we check Private to make sure variable is actually used in the shader (otherwise it won't be emitted if not part of all used variables in OpEntryPoint)
                    if (stream.Value.Private
                        && stream.Value.VariableMethodInitializerId is int methodInitializerId)
                    {
                        var variableValueType = ((PointerType)stream.Value.Type).BaseType;
                        buffer.FluentAdd(new OpFunctionCall(context.GetOrRegister(variableValueType), context.Bound++, methodInitializerId, []), out var methodInitializerCall);
                        buffer.Add(new OpStore(stream.Value.VariableId, methodInitializerCall.ResultId, null));
                    }
                }

                // Copy read variables from streams
                foreach (var stream in inputStreams)
                {
                    var baseType = ((PointerType)stream.Info.Type).BaseType;
                    buffer.FluentAdd(new OpLoad(context.Types[baseType], context.Bound++, stream.Id, null), out var loadedValue);
                    buffer.Add(new OpStore(stream.Info.VariableId, loadedValue.ResultId, null));
                }

                buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPointId, []));

                foreach (var stream in outputStreams)
                {
                    var baseType = ((PointerType)stream.Info.Type).BaseType;
                    buffer.FluentAdd(new OpLoad(context.Types[baseType], context.Bound++, stream.Info.VariableId, null), out var loadedValue);
                    buffer.Add(new OpStore(stream.Id, loadedValue.ResultId, null));
                }

                buffer.Add(new OpReturn());
                buffer.Add(new OpFunctionEnd());

                Span<int> pvariables = stackalloc int[inputStreams.Count + outputStreams.Count + privateStreams.Count + analysisResult.Blocks.Count + analysisResult.Resources.Count];
                int pvariableIndex = 0;
                foreach (var inputStream in inputStreams)
                    pvariables[pvariableIndex++] = inputStream.Id;
                foreach (var outputStream in outputStreams)
                    pvariables[pvariableIndex++] = outputStream.Id;
                foreach (var privateStream in privateStreams)
                    pvariables[pvariableIndex++] = privateStream.VariableId;
                // TODO: filter blocks and resources actually used by this entrypoint with ProcessMethod()?
                foreach (var block in analysisResult.Blocks)
                    pvariables[pvariableIndex++] = block;
                foreach (var resource in analysisResult.Resources)
                {
                    if (resource.Value.Used)
                        pvariables[pvariableIndex++] = resource.Key;
                }

                context.Add(new OpEntryPoint(executionModel, newEntryPointFunction, $"{entryPointName}_Wrapper", [.. pvariables.Slice(0, pvariableIndex)]));
            }

            return newEntryPointFunction;
        }

        /// <summary>
        /// Figure out (recursively) which streams are being read from and written to.
        /// </summary>
        private void AnalyzeStreamReadWrites(NewSpirvBuffer buffer, List<int> callStack, int functionId, AnalysisResult analysisResult)
        {
            var streams = analysisResult.Streams;
            var streamsAccessChains = new Dictionary<int, StreamInfo>();

            bool TryGetStream(int streamId, out StreamInfo streamInfo)
            {
                return streams.TryGetValue(streamId, out streamInfo)
                    || streamsAccessChains.TryGetValue(streamId, out streamInfo);
            }

            var methodStart = FindMethodStart(buffer, functionId);
            for (var index = methodStart; index < buffer.Count; index++)
            {
                var instruction = buffer[index];
                if (instruction.Op == Op.OpFunctionEnd)
                    break;

                if (instruction.Op is Op.OpLoad && (OpLoad)instruction is { } load)
                {
                    if (TryGetStream(load.Pointer, out var streamInfo) && !streamInfo.Write)
                        streamInfo.Read = true;
                    if (analysisResult.Resources.TryGetValue(load.Pointer, out var resourceInfo))
                        resourceInfo.Used = true;
                }
                else if (instruction.Op is Op.OpStore && (OpStore)instruction is { } store)
                {
                    if (TryGetStream(store.Pointer, out var streamInfo))
                        streamInfo.Write = true;
                    if (analysisResult.Resources.TryGetValue(store.Pointer, out var resourceInfo))
                        resourceInfo.Used = true;
                }
                else if (instruction is { Op: Op.OpAccessChain } && (OpAccessChain)instruction is { } accessChain)
                {
                    if (TryGetStream(accessChain.BaseId, out var streamInfo))
                    {
                        // Map the pointer access as access to the underlying stream (if any)
                        // i.e., streams.A.B will share same streamInfo as streams.A
                        // TODO: what happens in case of partial write?
                        streamsAccessChains.TryAdd(accessChain.ResultId, streamInfo);
                    }
                }
                else if (instruction.Op == Op.OpFunctionCall && (OpFunctionCall)instruction is { } call)
                {
                    // Process call
                    if (callStack.Contains(functionId))
                        throw new InvalidOperationException($"Recursive call with method id {functionId}");
                    callStack.Add(functionId);
                    AnalyzeStreamReadWrites(buffer, callStack, call.Function, analysisResult);
                    callStack.RemoveAt(callStack.Count - 1);
                }
            }
        }

        public int FindMethodStart(NewSpirvBuffer buffer, int functionId)
        {
            for (var index = 0; index < buffer.Count; index++)
            {
                var instruction = buffer[index];
                if (instruction.Op is Op.OpFunction && ((OpFunction)instruction).ResultId == functionId)
                    return index;
            }
            throw new InvalidOperationException($"Could not find start of method {functionId}");
        }
    }
}
