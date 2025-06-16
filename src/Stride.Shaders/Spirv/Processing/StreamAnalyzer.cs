using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;

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

        public void Process(SpirvBuffer buffer, SpirvContext context)
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
            GenerateStreamWrapper(buffer, context, Specification.ExecutionModel.Fragment, entryPointPS.Id, entryPointPS.Name, streams);

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

        private SortedList<int, (StreamInfo Stream, bool IsDirect)> CreateStreams(SpirvBuffer buffer, SpirvContext context)
        {
            var streams = new SortedList<int, (StreamInfo Stream, bool IsDirect)>();

            // Build name table
            SortedList<int, NameId> nameTable = [];
            SortedList<int, string> semanticTable = [];
            foreach (var instruction in buffer.Instructions)
            {
                {
                    if ((instruction.OpCode == SDSLOp.OpName || instruction.OpCode == SDSLOp.OpMemberName)
                        && instruction.TryGetOperand("target", out IdRef? target) && target is IdRef t
                        && instruction.TryGetOperand("name", out LiteralString? name) && name is LiteralString n
                       )
                    {
                        nameTable[t] = new(n.Value);
                    }
                }

                {
                    if (instruction.OpCode == SDSLOp.OpDecorateString
                        && instruction.UnsafeAs<InstOpDecorateString>().Decoration == Specification.Decoration.UserSemantic
                        && instruction.TryGetOperand("target", out IdRef? target) && target is IdRef t
                        && instruction.TryGetOperand("semanticName", out LiteralString? name) && name is LiteralString n
                       )
                    {
                        semanticTable[t] = n.Value;
                    }
                }
            }


            // Analyze streams
            foreach (var instruction in buffer.Instructions)
            {
                if (instruction.OpCode == SDSLOp.OpVariable
                    && (Specification.StorageClass)instruction.Operands[2] == Specification.StorageClass.Private)
                {
                    var name = nameTable.TryGetValue(instruction.Operands[1], out var nameId)
                        ? nameId.Name
                        : $"unnamed_{instruction.Operands[1]}";
                    var type = context.ReverseTypes[instruction.Operands[0]];
                    semanticTable.TryGetValue(instruction.Operands[1], out var semantic);
                    streams.Add(instruction.ResultId!.Value, (new StreamInfo(semantic, name, type, instruction.ResultId!.Value), true));
                }
            }

            return streams;
        }

        private void GenerateStreamWrapper(SpirvBuffer buffer, SpirvContext context, Specification.ExecutionModel executionModel, int entryPointId, string entryPointName, SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            ProcessMethod(buffer, entryPointId, streams);

            var stage = executionModel switch
            {
                Specification.ExecutionModel.Fragment => "PS",
                Specification.ExecutionModel.Vertex => "VS",
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
                    var pointerType = (IdRef)context.Buffer.AddOpTypePointer(context.Bound++, Specification.StorageClass.Input, context.Types[stream.Value.Stream.Type]);
                    var variable = context.Buffer.AddOpVariable(context.Bound++, pointerType, Specification.StorageClass.Input, null);
                    context.AddName(variable, $"in_{stream.Value.Stream.Name}");

                    if (stream.Value.Stream.Semantic != null)
                        context.Buffer.AddOpDecorateString(variable, Specification.Decoration.UserSemantic, null, null, stream.Value.Stream.Semantic);

                    inputStreams.Add((stream.Value.Stream, variable.ResultId.Value));
                }

                if (stream.Value.Stream.Output)
                {
                    var pointerType = (IdRef)context.Buffer.AddOpTypePointer(context.Bound++, Specification.StorageClass.Output, context.Types[stream.Value.Stream.Type]);
                    var variable = context.Buffer.AddOpVariable(context.Bound++, pointerType, Specification.StorageClass.Output, null);
                    context.AddName(variable, $"out_{stream.Value.Stream.Name}");

                    if (stream.Value.Stream.Semantic != null)
                        context.Buffer.AddOpDecorateString(variable, Specification.Decoration.UserSemantic, null, null, stream.Value.Stream.Semantic);

                    outputStreams.Add((stream.Value.Stream, variable.ResultId.Value));
                }
            }

            var voidTypeId = context.Buffer.AddOpTypeVoid(context.Bound++);

            // Add new entry point wrapper
            var newEntryPointFunctionType = context.Buffer.AddOpTypeFunction(context.Bound++, voidTypeId, []);
            var newEntryPointFunction = buffer.AddOpFunction(context.Bound++, voidTypeId, Specification.FunctionControlMask.None, newEntryPointFunctionType);
            buffer.AddOpLabel(context.Bound++);
            context.AddName(newEntryPointFunction, $"{entryPointName}_Wrapper");

            {
                // Copy read variables from streams
                foreach (var stream in inputStreams)
                {
                    var loadedValue = buffer.AddOpLoad(context.Bound++, context.Types[stream.Info.Type], stream.Id, null);
                    buffer.AddOpStore(stream.Info.Id, loadedValue.ResultId!.Value, null);
                }

                buffer.AddOpFunctionCall(context.Bound++, voidTypeId, entryPointId, Span<IdRef>.Empty);

                foreach (var stream in outputStreams)
                {
                    var loadedValue = buffer.AddOpLoad(context.Bound++, context.Types[stream.Info.Type], stream.Info.Id, null);
                    buffer.AddOpStore(stream.Id, loadedValue.ResultId!.Value, null);
                }

                buffer.AddOpReturn();
                buffer.AddOpFunctionEnd();

                Span<IdRef> pvariables = stackalloc IdRef[inputStreams.Count + outputStreams.Count];
                for (int i = 0; i < inputStreams.Count; i++)
                    pvariables[i] = inputStreams[i].Id;
                for (int i = 0; i < outputStreams.Count; i++)
                    pvariables[inputStreams.Count + i] = outputStreams[i].Id;
                context.Buffer.AddOpEntryPoint(executionModel, newEntryPointFunction, $"{entryPointName}_Wrapper", pvariables);
            }
        }

        /// <summary>
        /// Figure out (recursively) which streams are being read from and written to.
        /// </summary>
        private void ProcessMethod(SpirvBuffer buffer, int functionId, SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            var methodStart = FindMethodStart(buffer, functionId);
            for (var index = methodStart; index < buffer.Instructions.Count; index++)
            {
                var instruction = buffer.Instructions[index];
                if (instruction.OpCode == SDSLOp.OpFunctionEnd)
                    break;

                if (instruction.OpCode == SDSLOp.OpLoad
                    || instruction.OpCode == SDSLOp.OpStore)
                {
                    var operandIndex = instruction.OpCode == SDSLOp.OpLoad ? 2 : 0;
                    if (streams.TryGetValue(instruction.Operands[operandIndex], out var streamInfo))
                    {
                        // If read after a write (within same shader), we are not reading the variable from previous stage => not marking as Read
                        if (instruction.OpCode == SDSLOp.OpLoad && !streamInfo.Stream.Write)
                            streamInfo.Stream.Read = true;
                        if (instruction.OpCode == SDSLOp.OpStore)
                            streamInfo.Stream.Write = true;
                    }
                }
                else if (instruction.OpCode == SDSLOp.OpAccessChain)
                {
                    if (streams.TryGetValue(instruction.Operands[2], out var streamInfo))
                    {
                        // Map the pointer access as access to the underlying stream (if any)
                        // i.e., streams.A.B will share same streamInfo as streams.A
                        // TODO: what happens in case of partial write?
                        streams.Add(instruction.ResultId!.Value, (streamInfo.Stream, false));
                    }
                }
                else if (instruction.OpCode == SDSLOp.OpFunctionCall)
                {
                    // Process call
                    var calledFunctionId = instruction.Operands[2];
                    ProcessMethod(buffer, calledFunctionId, streams);
                }
            }
        }

        public int FindMethodStart(SpirvBuffer buffer, int functionId)
        {
            int? start = null;
            for (var index = 0; index < buffer.Instructions.Count; index++)
            {
                var instruction = buffer.Instructions[index];
                if (instruction.OpCode == SDSLOp.OpFunction
                    && instruction.ResultId == functionId)
                {
                    return index;
                }
            }

            throw new NotImplementedException();
        }
    }
}
