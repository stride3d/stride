using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing
{
    public class StreamAnalyzer
    {
        class StreamInfo(string? semantic, string name, SymbolType type, int id)
        {
            public string? Semantic { get; } = semantic;
            public string Name { get; } = name;
            public SymbolType Type { get; } = type;

            public int Id { get; } = id;

            /// <summary>
            /// We automatically mark input: a variable read before it's written to, or an output without a write.
            /// </summary>
            public bool Input => Read || (Output && !Write);
            public bool Output { get; set; }

            public bool Read { get; set; }
            public bool Write { get; set; }

            public override string ToString() => $"{Type} {Name} {(Read ? "R" : "")} {(Write ? "W" : "")}";
        }

        public void Process(NewSpirvBuffer buffer, SpirvContext context)
        {
            var entryPointVS = context.Module.Functions["VSMain"];
            var entryPointPS = context.Module.Functions["PSMain"];

            var streams = CreateStreams(buffer, context);

            // Expected at the end of pixel shader
            foreach (var stream in streams)
            {
                if (stream.Value.Stream.Semantic is { } semantic && (semantic.StartsWith("SV_Target") || semantic == "SV_Depth"))
                    stream.Value.Stream.Output = true;
            }
            GenerateStreamWrapper(buffer, context, ExecutionModel.Fragment, entryPointPS.Id, entryPointPS.Name, streams);

            // Those semantic variables are implicit in pixel shader, no need to forward them from previous stages
            foreach (var stream in streams)
            {
                if (stream.Value.Stream.Semantic is { } semantic && (semantic == "SV_Coverage" || semantic == "SV_IsFrontFace" || semantic == "VFACE"))
                    stream.Value.Stream.Read = false;
            }
            PropagateStreamsFromPreviousStage(streams);
            GenerateStreamWrapper(buffer, context, Specification.ExecutionModel.Vertex, entryPointVS.Id, entryPointVS.Name, streams);
        }

        private static void PropagateStreamsFromPreviousStage(SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            foreach (var stream in streams)
            {
                stream.Value.Stream.Output = stream.Value.Stream.Read;
                stream.Value.Stream.Read = false;
                stream.Value.Stream.Write = false;
            }
        }

        private SortedList<int, (StreamInfo Stream, bool IsDirect)> CreateStreams(NewSpirvBuffer buffer, SpirvContext context)
        {
            var streams = new SortedList<int, (StreamInfo Stream, bool IsDirect)>();

            // Build name table
            SortedList<int, NameId> nameTable = [];
            SortedList<int, string> semanticTable = [];
            foreach (var instruction in buffer)
            {
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

                {
                    if (instruction.Op == Op.OpDecorateString
                        && ((OpDecorateString)instruction) is
                        {
                            Decoration: Decoration.UserSemantic,
                            Target: int t,
                            AdditionalString: string n
                        }
                       )
                    {
                        semanticTable[t] = n;
                    }
                }
            }


            // Analyze streams
            foreach (var instruction in buffer)
            {
                if (instruction.Op == Op.OpVariable && ((OpVariable)instruction) is
                    {
                        Storageclass: StorageClass.Private,
                        ResultId: int
                    } variable
                   )
                {
                    var name = nameTable.TryGetValue(variable.ResultId, out var nameId)
                        ? nameId.Name
                        : $"unnamed_{variable.ResultId}";
                    var type = context.ReverseTypes[variable.ResultType];
                    semanticTable.TryGetValue(variable.ResultId, out var semantic);
                    streams.Add(variable.ResultId, (new StreamInfo(semantic, name, type, variable.ResultId), true));
                }
            }

            return streams;
        }

        private void GenerateStreamWrapper(NewSpirvBuffer buffer, SpirvContext context, ExecutionModel executionModel, int entryPointId, string entryPointName, SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            ProcessMethod(buffer, entryPointId, streams);

            var stage = executionModel switch
            {
                ExecutionModel.Fragment => "PS",
                ExecutionModel.Vertex => "VS",
                _ => throw new NotImplementedException()
            };
            List<(StreamInfo Info, int Id)> inputStreams = [];
            List<(StreamInfo Info, int Id)> outputStreams = [];
            foreach (var stream in streams)
            {
                // Only direct access to global variables (not temporary variables created within function)
                if (!stream.Value.IsDirect)
                    continue;

                if (stream.Value.Stream.Input)
                {
                    context.FluentAdd(new OpTypePointer(context.Bound++, StorageClass.Input, context.Types[stream.Value.Stream.Type]), out var pointerType);
                    context.FluentAdd(new OpVariable(pointerType.ResultId, context.Bound++, StorageClass.Input, null), out var variable);
                    context.AddName(variable, $"in_{stream.Value.Stream.Name}");

                    if (stream.Value.Stream.Semantic != null)
                        context.Add(new OpDecorateString(variable, Decoration.UserSemantic, stream.Value.Stream.Semantic));

                    inputStreams.Add((stream.Value.Stream, variable.ResultId));
                }

                if (stream.Value.Stream.Output)
                {
                    context.FluentAdd(new OpTypePointer(context.Bound++, StorageClass.Output, context.Types[stream.Value.Stream.Type]), out var pointerType)
                    .FluentAdd(new OpVariable(context.Bound++, pointerType, StorageClass.Output, null), out var variable);
                    context.AddName(variable, $"out_{stream.Value.Stream.Name}");

                    if (stream.Value.Stream.Semantic != null)
                        context.Add(new OpDecorateString(variable, Decoration.UserSemantic, stream.Value.Stream.Semantic));

                    outputStreams.Add((stream.Value.Stream, variable.ResultId));
                }
            }

            context.FluentAdd(new OpTypeVoid(context.Bound++), out var voidType);

            // Add new entry point wrapper
            context.FluentAdd(new OpTypeFunction(context.Bound++, voidType, []), out var newEntryPointFunctionType);
            buffer.FluentAdd(new OpFunction(voidType, context.Bound++, FunctionControlMask.None, newEntryPointFunctionType) , out var newEntryPointFunction);
            buffer.Add(new OpLabel(context.Bound++));
            context.AddName(newEntryPointFunction, $"{entryPointName}_Wrapper");

            {
                // Copy read variables from streams
                foreach (var stream in inputStreams)
                {
                    var baseType = ((PointerType)stream.Info.Type).BaseType;
                    buffer.FluentAdd(new OpLoad(context.Types[baseType], context.Bound++, stream.Id, null), out var loadedValue);
                    buffer.Add(new OpStore(stream.Info.Id, loadedValue.ResultId, null));
                }

                buffer.Add(new OpFunctionCall(voidType, context.Bound++, entryPointId, []));

                foreach (var stream in outputStreams)
                {
                    var baseType = ((PointerType)stream.Info.Type).BaseType;
                    buffer.FluentAdd(new OpLoad(context.Bound++, context.Types[baseType], stream.Info.Id, null), out var loadedValue);
                    buffer.Add(new OpStore(stream.Id, loadedValue.ResultId, null));
                }

                buffer.Add(new OpReturn());
                buffer.Add(new OpFunctionEnd());

                Span<int> pvariables = stackalloc int[inputStreams.Count + outputStreams.Count];
                for (int i = 0; i < inputStreams.Count; i++)
                    pvariables[i] = inputStreams[i].Id;
                for (int i = 0; i < outputStreams.Count; i++)
                    pvariables[inputStreams.Count + i] = outputStreams[i].Id;
                context.Add(new OpEntryPoint(executionModel, newEntryPointFunction, $"{entryPointName}_Wrapper", [..pvariables]));
            }
        }

        /// <summary>
        /// Figure out (recursively) which streams are being read from and written to.
        /// </summary>
        private void ProcessMethod(NewSpirvBuffer buffer, int functionId, SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            var methodStart = FindMethodStart(buffer, functionId);
            for (var index = methodStart; index < buffer.Count; index++)
            {
                var instruction = buffer[index];
                if (instruction.Op == Op.OpFunctionEnd)
                    break;

                if (instruction.Op is Op.OpLoad && (OpLoad)instruction is { } load)
                {
                    if (streams.TryGetValue(load.Pointer, out var streamInfo) && !streamInfo.Stream.Write)
                        streamInfo.Stream.Read = true;
                }
                else if(instruction.Op is Op.OpStore && (OpStore)instruction is { } store)
                {
                    if (streams.TryGetValue(store.Pointer, out var streamInfo))
                        streamInfo.Stream.Write = true;
                }
                else if (instruction is { Op: Op.OpAccessChain } && (OpAccessChain)instruction is { } accessChain)
                {
                    if (streams.TryGetValue(accessChain.BaseId, out var streamInfo))
                    {
                        // Map the pointer access as access to the underlying stream (if any)
                        // i.e., streams.A.B will share same streamInfo as streams.A
                        // TODO: what happens in case of partial write?
                        streams.Add(accessChain.ResultId, (streamInfo.Stream, false));
                    }
                }
                else if (instruction.Op == Op.OpFunctionCall && (OpFunctionCall)instruction is { } call)
                {
                    // Process call
                    ProcessMethod(buffer, call.Function, streams);
                }
            }
        }

        public int FindMethodStart(NewSpirvBuffer buffer, int functionId)
        {
            for (var index = 0; index < buffer.Count; index++)
            {
                var instruction = buffer[index];
                if (instruction.Op is Op.OpFunction)
                    return index;
            }
            throw new NotImplementedException();
        }
    }
}
