using Spv;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public void Process(SymbolTable table, CompilerUnit compiler)
        {
            var context = compiler.Context;

            var entryPointVS = context.Module.Functions["VSMain"];
            var entryPointPS = context.Module.Functions["PSMain"];

            var streams = CreateStreams(compiler);

            // Expected at the end of pixel shader
            foreach (var stream in streams)
            {
                if (stream.Value.Stream.Semantic is { } semantic && (semantic.StartsWith("SV_Target") || semantic == "SV_Depth"))
                    stream.Value.Stream.Output = true;
            }
            GenerateStreamWrapper(table, compiler, Specification.ExecutionModel.Fragment, entryPointPS.Id, entryPointPS.Name, streams);

            // Those semantic variables are implicit in pixel shader, no need to forward them from previous stages
            foreach (var stream in streams)
            {
                if (stream.Value.Stream.Semantic is { } semantic && (semantic == "SV_Coverage" || semantic == "SV_IsFrontFace" || semantic == "VFACE"))
                    stream.Value.Stream.Read = false;
            }
            PropagateStreamsFromPreviousStage(streams);
            GenerateStreamWrapper(table, compiler, Specification.ExecutionModel.Vertex, entryPointVS.Id, entryPointVS.Name, streams);
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

        private SortedList<int, (StreamInfo Stream, bool IsDirect)> CreateStreams(CompilerUnit compiler)
        {
            var context = compiler.Context;
            var streams = new SortedList<int, (StreamInfo Stream, bool IsDirect)>();

            // Build name table
            SortedList<int, NameId> nameTable = [];
            SortedList<int, string> semanticTable = [];
            foreach (var instruction in context.Buffer.Instructions)
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
            foreach (var instruction in compiler.Context.Buffer.Instructions)
            {
                if (instruction.OpCode == SDSLOp.OpVariable
                    && (Specification.StorageClass)instruction.Operands[2] == Specification.StorageClass.Private)
                {
                    var name = nameTable.TryGetValue(instruction.Operands[1], out var nameId)
                        ? nameId.Name
                        : $"unnamed_{instruction.Operands[1]}";
                    var type = compiler.Context.ReverseTypes[instruction.Operands[0]];
                    semanticTable.TryGetValue(instruction.Operands[1], out var semantic);
                    streams.Add(instruction.ResultId!.Value, (new StreamInfo(semantic, name, type, instruction.ResultId!.Value), true));
                }
            }

            return streams;
        }

        private void GenerateStreamWrapper(SymbolTable table, CompilerUnit compiler, Specification.ExecutionModel executionModel, int entryPointId, string entryPointName, SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            ProcessMethod(compiler, entryPointId, streams);

            var stage = executionModel switch
            {
                Specification.ExecutionModel.Fragment => "PS",
                Specification.ExecutionModel.Vertex => "VS",
            };
            var context = compiler.Context;
            List<StreamInfo> inputStreams = [];
            List<StreamInfo> outputStreams = [];
            foreach (var stream in streams)
            {
                // Only direct access to global variables (not temporary variables created within function)
                if (!stream.Value.IsDirect)
                    continue;

                if (stream.Value.Stream.Input)
                    inputStreams.Add(stream.Value.Stream);
                // TODO: filter with previous stage
                if (stream.Value.Stream.Output)
                    outputStreams.Add(stream.Value.Stream);
            }

            var voidTypeId = context.Buffer.AddOpTypeVoid(context.Bound++);

            Span<IdRef> inputFields = stackalloc IdRef[inputStreams.Count];
            int inputFieldIndex = 0;
            var inputStructId = context.Bound++;
            foreach (var stream in inputStreams)
            {
                inputFields[inputFieldIndex] = context.Types[stream.Type];
                context.Buffer.AddOpMemberName(inputStructId, inputFieldIndex++, stream.Name);
            }
            context.Buffer.AddOpTypeStruct(inputStructId, inputFields);
            context.AddName(inputStructId, $"{stage}_INPUT");
            var inputStructPtrId = (IdRef)context.Buffer.AddOpTypePointer(context.Bound++, Specification.StorageClass.Function, inputStructId);

            Span<IdRef> outputFields = stackalloc IdRef[outputStreams.Count];
            int outputFieldIndex = 0;
            var outputStructId = context.Bound++;
            foreach (var stream in outputStreams)
            {
                outputFields[outputFieldIndex] = context.Types[stream.Type];
                context.Buffer.AddOpMemberName(outputStructId, outputFieldIndex++, stream.Name);
            }
            context.Buffer.AddOpTypeStruct(outputStructId, outputFields);
            context.AddName(outputStructId, $"{stage}_OUTPUT");
            var outputStructPtrId = context.Buffer.AddOpTypePointer(context.Bound++, Specification.StorageClass.Function, outputStructId);

            // Add new entry point wrapper
            var newEntryPointFunctionType = context.Buffer.AddOpTypeFunction(context.Bound++, outputStructId, MemoryMarshal.CreateSpan(ref inputStructPtrId, 1));
            var newEntryPointFunction = compiler.Builder.Buffer.AddOpFunction(context.Bound++, outputStructId, Specification.FunctionControlMask.None, newEntryPointFunctionType);
            context.AddName(newEntryPointFunction, $"{entryPointName}_Wrapper");

            {
                // Add INPUT (as a function parameter)
                var inputParameter = compiler.Builder.Buffer.AddOpFunctionParameter(context.Bound++, inputStructPtrId);
                context.AddName(inputParameter, "input");

                compiler.Builder.Buffer.AddOpLabel(context.Bound++);

                // Add OUTPUT (as a local variable)
                var outputParameter = compiler.Builder.Buffer.AddOpVariable(context.Bound++, outputStructPtrId.ResultId.Value, Specification.StorageClass.Function, null);
                context.AddName(outputParameter, "output");

                // Copy read variables from streams
                inputFieldIndex = 0;
                foreach (var stream in inputStreams)
                {
                    var typeId = compiler.Context.GetOrRegister(stream.Type);
                    var typePtrId = compiler.Context.GetOrRegister(new PointerType(stream.Type));
                    var indexLiteral = new IntegerLiteral(new(32, false, true), inputFieldIndex++, new());
                    indexLiteral.ProcessSymbol(table);
                    IdRef indexIdRef = compiler.Context.CreateConstant(indexLiteral).Id;
                    var accessChain = compiler.Builder.Buffer.AddOpAccessChain(compiler.Context.Bound++, typeId, inputParameter.ResultId!.Value, MemoryMarshal.CreateSpan(ref indexIdRef, 1));
                    var loadedValue = compiler.Builder.Buffer.AddOpLoad(context.Bound++, context.Types[stream.Type], accessChain, null);

                    compiler.Builder.Buffer.AddOpStore(stream.Id, loadedValue.ResultId!.Value, null);
                }

                compiler.Builder.Buffer.AddOpFunctionCall(context.Bound++, voidTypeId, entryPointId, Span<IdRef>.Empty);

                inputFieldIndex = 0;
                foreach (var stream in outputStreams)
                {
                    var loadedValue = compiler.Builder.Buffer.AddOpLoad(context.Bound++, context.Types[stream.Type], stream.Id, null);
                    var typeId = compiler.Context.GetOrRegister(stream.Type);
                    var typePtrId = compiler.Context.GetOrRegister(new PointerType(stream.Type));
                    var indexLiteral = new IntegerLiteral(new(32, false, true), inputFieldIndex++, new());
                    indexLiteral.ProcessSymbol(table);
                    IdRef indexIdRef = compiler.Context.CreateConstant(indexLiteral).Id;
                    var accessChain = compiler.Builder.Buffer.AddOpAccessChain(compiler.Context.Bound++, typeId, outputParameter.ResultId!.Value, MemoryMarshal.CreateSpan(ref indexIdRef, 1));

                    compiler.Builder.Buffer.AddOpStore(accessChain.ResultId!.Value, loadedValue.ResultId!.Value, null);
                }

                var outputResult = compiler.Builder.Buffer.AddOpLoad(context.Bound++, outputStructId, outputParameter, null);
                compiler.Builder.Buffer.AddOpReturnValue(outputResult);
                compiler.Builder.Buffer.AddOpFunctionEnd();
            }

            context.SetEntryPoint(executionModel, newEntryPointFunction, $"{entryPointName}_Wrapper", []);
        }

        /// <summary>
        /// Figure out (recursively) which streams are being read from and written to.
        /// </summary>
        private void ProcessMethod(CompilerUnit compiler, int functionId, SortedList<int, (StreamInfo Stream, bool IsDirect)> streams)
        {
            var methodStart = FindMethodStart(compiler, functionId);
            for (var index = methodStart; index < compiler.Builder.Buffer.Instructions.Count; index++)
            {
                var instruction = compiler.Builder.Buffer.Instructions[index];
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
                    ProcessMethod(compiler, calledFunctionId, streams);
                }
            }
        }

        public int FindMethodStart(CompilerUnit compiler, int functionId)
        {
            int? start = null;
            for (var index = 0; index < compiler.Builder.Buffer.Instructions.Count; index++)
            {
                var instruction = compiler.Builder.Buffer.Instructions[index];
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
