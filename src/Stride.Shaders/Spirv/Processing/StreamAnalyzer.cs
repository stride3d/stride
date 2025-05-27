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
        class StreamInfo(string name, SymbolType type)
        {
            public string Name { get; } = name;
            public SymbolType Type { get; } = type;

            public int FieldIndex { get; set; } = -1;
            public bool Read { get; set; }
            public bool Write { get; set; }

            public override string ToString() => $"{Type} {Name} {(Read ? "R" : "")} {(Write ? "W" : "")}";
        }

        public void Process(SymbolTable table, CompilerUnit compiler, SpirvFunction entryPoint)
        {
            var context = compiler.Context;
            var streams = new Dictionary<int, StreamInfo>();

            // Build name table
            SortedList<int, NameId> nameTable = [];
            foreach (var instruction in compiler.Context.Buffer)
            {
                if ((instruction.OpCode == SDSLOp.OpName || instruction.OpCode == SDSLOp.OpMemberName)
                    && instruction.TryGetOperand("target", out IdRef? target) && target is IdRef t
                    && instruction.TryGetOperand("name", out LiteralString? name) && name is LiteralString n
                    )
                {
                    nameTable[t] = new(n.Value);
                }
            }


            // Analyze streams
            foreach (var instruction in compiler.Context.Buffer)
            {
                if (instruction.OpCode == SDSLOp.OpVariable
                    && (Specification.StorageClass)instruction.Operands[2] == Specification.StorageClass.Private)
                {
                    var name = nameTable.TryGetValue(instruction.Operands[1], out var nameId)
                        ? nameId.Name
                        : $"unnamed_{instruction.Operands[1]}";
                    var type = compiler.Context.ReverseTypes[instruction.Operands[0]];
                    streams.Add(instruction.ResultId!.Value, new StreamInfo(name, type));
                }
            }

            // Create streams struct
            //var streamStructType = new StructType("STREAMS", streams);
            //var streamStruct = compiler.Context.GetOrRegister(streamStructType);

            var streamStructId = context.Bound++;
            var streamStructPtrId = context.Buffer.AddOpTypePointer(context.Bound++, Specification.StorageClass.Function, streamStructId);

            //for (var index = 0; index < structSymbol.Fields.Count; index++)
            //    AddMemberName(result, index, structSymbol.Fields[index].Name);

            List<int> structStreams = new();
            int totalActiveStreams = 0;
            ProcessMethod(table, compiler, entryPoint.Id, true, streamStructPtrId.ResultId!.Value, streams, ref totalActiveStreams);

            Span<IdRef> fields = stackalloc IdRef[totalActiveStreams];
            foreach (var stream in streams)
            {
                if (stream.Value.FieldIndex == -1)
                    continue;
                fields[stream.Value.FieldIndex] = context.Types[stream.Value.Type];
                context.Buffer.AddOpMemberName(streamStructId, stream.Value.FieldIndex, stream.Value.Name);
            }
            var result = context.Buffer.AddOpTypeStruct(streamStructId, fields);
            context.AddName(result, "STREAMS");

            var methodStart = FindMethodStart(compiler, entryPoint.Id);
            var enumerator = new RefMutableFunctionInstructionEnumerator(compiler.Builder.Buffer, methodStart);
        }

        private bool ProcessMethod(SymbolTable table, CompilerUnit compiler, int functionId, bool isEntryPoint, int streamStructPtrId, Dictionary<int, StreamInfo> streams, ref int totalActiveStreams)
        {
            var methodStart = FindMethodStart(compiler, functionId);
            int? streamsVariableId = null;
            var enumerator = new RefMutableFunctionInstructionEnumerator(compiler.Builder.Buffer, methodStart);
            var context = compiler.Context;

            void MarkStreamsUsed()
            {
                if (streamsVariableId == null)
                {
                    streamsVariableId = context.Bound++;
                }
            }

            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;

                if (instruction.OpCode == SDSLOp.OpLoad
                    || instruction.OpCode == SDSLOp.OpStore)
                {
                    var operandIndex = instruction.OpCode == SDSLOp.OpLoad ? 2 : 0;
                    if (streams.TryGetValue(instruction.Operands[operandIndex], out var streamInfo))
                    {
                        if (streamInfo.FieldIndex == -1)
                        {
                            streamInfo.FieldIndex = totalActiveStreams++;
                            // First time, let's register it
                        }

                        if (instruction.OpCode == SDSLOp.OpLoad && !streamInfo.Write)
                            streamInfo.Read = true;
                        if (instruction.OpCode == SDSLOp.OpStore)
                            streamInfo.Write = true;
                        MarkStreamsUsed();

                        var typeId = compiler.Context.GetOrRegister(streamInfo.Type);
                        var typePtrId = compiler.Context.GetOrRegister(new PointerType(streamInfo.Type));
                        var index = streamInfo.FieldIndex;
                        var indexLiteral = new IntegerLiteral(new(32, false, true), index, new());
                        indexLiteral.ProcessSymbol(table);
                        IdRef indexIdRef = compiler.Context.CreateConstant(indexLiteral).Id;
                        var accessChain = compiler.Builder.Buffer.InsertOpAccessChain(instruction.WordIndex, compiler.Context.Bound++, typePtrId, streamsVariableId!.Value, MemoryMarshal.CreateSpan(ref indexIdRef, 1));

                        // Update OpLoad/OpStore to use the new OpAccessChain
                        enumerator.MoveNext();
                        instruction = enumerator.Current;
                        instruction.Operands[operandIndex] = accessChain.ResultId!.Value;
                    }
                }
                else if (instruction.OpCode == SDSLOp.OpAccessChain)
                {
                    if (streams.TryGetValue(instruction.Operands[2], out var streamInfo))
                    {
                        // Map the pointer access as access to the underlying stream (if any)
                        // i.e., streams.A.B will share same streamInfo as streams.A
                        // TODO: need to store access chain, handle type info in OpStore/OpLoad, etc.
                        streams.Add(instruction.ResultId!.Value, streamInfo);

                        // TODO: Add OpAccessChain entry
                        //throw new NotImplementedException();
                    }
                }
                else if (instruction.OpCode == SDSLOp.OpFunctionCall)
                {
                    // Process call
                    var calledFunctionId = instruction.Operands[2];
                    if (ProcessMethod(table, compiler, calledFunctionId, false, streamStructPtrId, streams, ref totalActiveStreams))
                        MarkStreamsUsed();
                }
            }

            if (streamsVariableId != null)
            {
                enumerator = new RefMutableFunctionInstructionEnumerator(compiler.Builder.Buffer, methodStart);

                while (enumerator.MoveNext())
                {
                    var instruction = enumerator.Current;

                    if (instruction.OpCode == SDSLOp.OpFunction)
                    {
                        // Entry point will add STREAMS as a variable
                        // For other functions, it will be passed through
                        if (isEntryPoint)
                        {
                            while ((instruction.OpCode == SDSLOp.OpFunction || instruction.OpCode == SDSLOp.OpFunctionParameter || instruction.OpCode == SDSLOp.OpLabel) && enumerator.MoveNext())
                                instruction = enumerator.Current;

                            var streamsVariable = compiler.Builder.Buffer.InsertOpVariable(instruction.WordIndex, streamsVariableId.Value, streamStructPtrId, Specification.StorageClass.Function, null);
                            enumerator.MoveNext();
                            instruction = enumerator.Current;

                            context.AddName(streamsVariable, "streams");

                            // Copy read variables from streams
                            foreach (var streamInfo in streams)
                            {
                                if (streamInfo.Value.Read)
                                {
                                    var loadedValue = compiler.Builder.Buffer.InsertOpLoad(instruction.WordIndex, context.Bound++, context.Types[streamInfo.Value.Type], streamInfo.Key, null);
                                    enumerator.MoveNext();
                                    instruction = enumerator.Current;
                                    var typeId = compiler.Context.GetOrRegister(streamInfo.Value.Type);
                                    var typePtrId = compiler.Context.GetOrRegister(new PointerType(streamInfo.Value.Type));
                                    var index = streamInfo.Value.FieldIndex;
                                    var indexLiteral = new IntegerLiteral(new(32, false, true), index, new());
                                    indexLiteral.ProcessSymbol(table);
                                    IdRef indexIdRef = compiler.Context.CreateConstant(indexLiteral).Id;
                                    var accessChain = compiler.Builder.Buffer.InsertOpAccessChain(instruction.WordIndex, compiler.Context.Bound++, typeId, streamsVariableId!.Value, MemoryMarshal.CreateSpan(ref indexIdRef, 1));
                                    enumerator.MoveNext();
                                    instruction = enumerator.Current;

                                    compiler.Builder.Buffer.InsertOpStore(instruction.WordIndex, accessChain.ResultId!.Value, loadedValue.ResultId!.Value, null);
                                }
                            }
                        }
                        else
                        {
                            var functionType = context.Buffer.FindInstructionByResultId(instruction.Operands[3]);
                            Span<IdRef> parameterTypes = stackalloc IdRef[1 + functionType.Operands.Length - 2];
                            parameterTypes[0] = streamStructPtrId;
                            for (int i = 0; i < functionType.Operands.Length - 2; ++i)
                                parameterTypes[i + 1] = instruction.Operands[2 + i];
                            var newFunctionType = compiler.Context.Buffer.InsertOpTypeFunction(functionType.WordIndex + functionType.WordCount, context.Bound++, functionType.Operands[1], parameterTypes);

                            // Update function type
                            instruction.Operands[3] = newFunctionType.ResultId!.Value;

                            var streamsVariable = compiler.Builder.Buffer.InsertOpFunctionParameter(instruction.WordIndex + instruction.WordCount, streamsVariableId.Value, streamStructPtrId);
                        }
                    }
                }
            }

            return streamsVariableId != null;
        }

        public int FindMethodStart(CompilerUnit compiler, int functionId)
        {
            int? start = null;
            foreach (var instruction in compiler.Builder.Buffer)
            {
                if (instruction.OpCode == SDSLOp.OpFunction
                    && instruction.ResultId == functionId)
                {
                    return instruction.WordIndex;
                }
            }

            throw new NotImplementedException();
        }
    }
}
