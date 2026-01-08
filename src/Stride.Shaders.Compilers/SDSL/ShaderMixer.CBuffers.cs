using CommunityToolkit.HighPerformance.Buffers;
using Stride.Core.Extensions;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static Stride.Shaders.Spirv.Specification;
using StorageClass = Stride.Shaders.Parsing.SDSL.AST.StorageClass;

namespace Stride.Shaders.Compilers.SDSL
{
    partial class ShaderMixer
    {
        private void GenerateDefaultCBuffer(MixinNode rootMixin, MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer temp)
        {
            var members = new List<StructuredTypeMember>();
            // Remap from variable ID to member index in our new struct
            var variableToMemberIndices = new Dictionary<int, int>(); 
            // Collect any variable not a stream, not static and not a block
            HashSet<int> blockTypes = [];
            int firstVariableIndex = -1;
            foreach (var i in temp)
            {
                if (i.Op == Op.OpVariableSDSL
                    && ((OpVariableSDSL)i) is { Storageclass: Specification.StorageClass.Uniform } variable
                    && context.ReverseTypes[variable.ResultType] is PointerType { BaseType: var variableType }
                    && variableType is not ConstantBufferSymbol)
                {
                    firstVariableIndex = i.Index;
                    variableToMemberIndices.Add(variable.ResultId, members.Count);
                    members.Add(new(context.Names[variable.ResultId], variableType, TypeModifier.None));
                    SetOpNop(i.Data.Memory.Span);
                }
            }

            // No global members? Let's finish now
            if (members.Count == 0)
                return;

            var globalCBufferType = new ConstantBufferSymbol("Globals", members);
            var globalCBufferTypeId = context.DeclareCBuffer(globalCBufferType);
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                context.AddMemberName(globalCBufferTypeId, index, member.Name);
            }
            
            // Note: we make sure to add at a previous variable index, otherwise the OpVariableSDSL won't be inside the root MixinNode.StartInstruction/EndInstruction
            temp.FluentReplace(firstVariableIndex, new OpVariableSDSL(context.GetOrRegister(new PointerType(globalCBufferType, Specification.StorageClass.Uniform)), context.Bound++, Specification.StorageClass.Uniform, VariableFlagsMask.Stage, null), out var cbufferVariable);
            context.AddName(cbufferVariable.ResultId, "Globals");
            
            // Replace all accesses
            int instructionsAddedInThisMethod = 0;
            for (var index = 0; index < temp.Count; index++)
            {
                var i = temp[index];
                if (i.Op == Op.OpFunctionEnd)
                {
                    // Since we might have inserted instructions, offset all Start/End instructions indices
                    if (instructionsAddedInThisMethod > 0)
                        AdjustIndicesAfterAppendInstructions(rootMixin, i.Index, instructionsAddedInThisMethod);
                    instructionsAddedInThisMethod = 0;
                }
                if (i.Op is Op.OpLoad && (OpLoad)i is { } load)
                {
                    if (variableToMemberIndices.TryGetValue(load.Pointer, out var memberIndex))
                    {
                        load.Pointer = context.Bound;
                        instructionsAddedInThisMethod++;
                        temp.Insert(index++, new OpAccessChain(
                            context.GetOrRegister(new PointerType(members[memberIndex].Type, Specification.StorageClass.Uniform)),
                            context.Bound++,
                            cbufferVariable.ResultId,
                            [context.CompileConstant(memberIndex).Id]));
                    }
                }
                else if (i.Op is Op.OpStore && (OpStore)i is { } store)
                {
                    if (variableToMemberIndices.TryGetValue(store.Pointer, out var memberIndex))
                    {
                        store.Pointer = context.Bound;
                        instructionsAddedInThisMethod++;
                        temp.Insert(index++, new OpAccessChain(
                            context.GetOrRegister(new PointerType(members[memberIndex].Type, Specification.StorageClass.Uniform)),
                            context.Bound++,
                            cbufferVariable.ResultId,
                            [context.CompileConstant(memberIndex).Id]));
                    }
                }
                else if (i.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain)
                {
                    if (variableToMemberIndices.TryGetValue(accessChain.BaseId, out var memberIndex))
                    {
                        accessChain.Values = new([context.CompileConstant(memberIndex).Id, ..accessChain.Values.Elements.Span]);
                        accessChain.BaseId = cbufferVariable.ResultId;
                    }
                }
            }

            // Update entry points to include this cbuffer
            foreach (var i in context)
            {
                if (i.Op == Op.OpEntryPoint && (OpEntryPoint)i is {} entryPoint)
                {
                    entryPoint.Values = new([..entryPoint.Values, cbufferVariable.ResultId]);
                }
            }
            
            // Remap decorations
            foreach (var i in context)
            {
                if (i.Op == Op.OpDecorate && (OpDecorate)i is {} decorate)
                {
                    if (variableToMemberIndices.TryGetValue(decorate.Target, out var memberIndex))
                        i.Buffer.Replace(i.Index, new OpMemberDecorate(globalCBufferTypeId, memberIndex, decorate.Decoration));
                }
                else if (i.Op == Op.OpDecorateString && (OpDecorateString)i is {} decorateString)
                {
                    if (variableToMemberIndices.TryGetValue(decorateString.Target, out var memberIndex))
                        i.Buffer.Replace(i.Index, new OpMemberDecorateString(globalCBufferTypeId, memberIndex, decorateString.Decoration));
                }
            }
        }

        private static void MergeCBuffers(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer)
        {
            // Collect Decorations
            Dictionary<int, string> logicalGroups = new();
            Dictionary<(int StructType, int Member), (Dictionary<Decoration, string> StringDecorations, Dictionary<Decoration, MemoryOwner<int>> Decorations)> decorations = new();
            foreach (var i in context)
            {
                if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Parameters: var m } } memberDecorate)
                {
                    using var n = new LiteralValue<string>(m.Span);
                    if (!decorations.TryGetValue((memberDecorate.StructType, memberDecorate.Member), out var decorationsForThisMember))
                        decorations.Add((memberDecorate.StructType, memberDecorate.Member), decorationsForThisMember = new(new(), new()));
                    decorationsForThisMember.StringDecorations.Add(memberDecorate.Decoration.Value, n.Value);
                }
                else if (i.Op == Op.OpMemberDecorate && (OpMemberDecorate)i is { Decoration: { Parameters: var m2 } } memberDecorate2)
                {
                    if (!decorations.TryGetValue((memberDecorate2.StructureType, memberDecorate2.Member), out var decorationsForThisMember))
                        decorations.Add((memberDecorate2.StructureType, memberDecorate2.Member), decorationsForThisMember = new(new(), new()));
                    decorationsForThisMember.Decorations.Add(memberDecorate2.Decoration.Value, m2);
                }
                else if (i.Op == Op.OpDecorateString && (OpDecorateString)i is { Decoration: { Value: Decoration.LogicalGroupSDSL, Parameters: var m3 } } decorateLogicalGroup)
                {
                    using var n = new LiteralValue<string>(m3.Span);
                    logicalGroups.Add(decorateLogicalGroup.Target, n.Value);
                }
            }

            string? GetCBufferLogicalGroup(int variableId)
            {
                logicalGroups.TryGetValue(variableId, out var logicalGroup);
                return logicalGroup;
            }

            // OpSDSLEffect is emitted for any non-root composition
            var compositionNodes = buffer
                .Where(x => x.Op == Op.OpSDSLComposition)
                .Select(x => (StartIndex: x.Index, CompositionPath: ((OpSDSLComposition)x).CompositionPath))
                .ToList();

            var shaders = buffer
                .Where(x => x.Op == Op.OpSDSLShader)
                .Select(x => (StartIndex: x.Index, ShaderName: ((OpSDSLShader)x).ShaderName))
                .ToList();

            var cbuffersByNames = buffer
                .Where(x => x.Op == Op.OpVariableSDSL)
                .Select(x => (Index: x.Index, Variable: x))
                // Note: MemberIndexOffset is simply a shift in Members index, not something like a byte offset
                .Select(x => (
                    Variable: x.Variable,
                    CompositionPath: compositionNodes.LastOrDefault(mixinNode => x.Index >= mixinNode.StartIndex).CompositionPath,
                    ShaderName: shaders.LastOrDefault(shader => x.Index >= shader.StartIndex).ShaderName,
                    StructTypePtrId: x.Variable.Data.IdResultType.Value,
                    StructType: context.ReverseTypes[x.Variable.Data.IdResultType.Value] is PointerType p && p.StorageClass == Specification.StorageClass.Uniform && p.BaseType is StructuredType s ? s : null,
                    MemberIndexOffset: 0,
                    LogicalGroup: GetCBufferLogicalGroup(x.Variable.Data.IdResult.Value)))
                // TODO: Check Decoration.Block?
                .Where(x => x.StructType != null)
                .GroupBy(x => ShaderClass.GetCBufferRealName(context.Names[x.Variable.Data.IdResult.Value]));

            var cbufferStructTypes = cbuffersByNames.SelectMany(x => x).Select(x => context.Types[x.StructType]).ToHashSet();

            void ProcessDecorations(Span<(OpDataIndex Variable, string CompositionPath, string ShaderName, int StructTypePtrId, StructuredType? StructType, int MemberIndexOffset, string LogicalGroup)> cbuffersSpan, int cbufferStructId, bool newStructure)
            {
                int mergedMemberIndex = 0;
                foreach (ref var cbuffer in cbuffersSpan)
                {
                    var compositionPath = cbuffer.CompositionPath;

                    for (int memberIndex = 0; memberIndex < cbuffer.StructType.Members.Count; memberIndex++, mergedMemberIndex++)
                    {
                        var member = cbuffer.StructType.Members[memberIndex];

                        if (!decorations.TryGetValue((context.Types[cbuffer.StructType], memberIndex), out var decorationsForThisMember))
                            decorations.Add((context.Types[cbuffer.StructType], memberIndex), decorationsForThisMember = new(new(), new()));

                        decorationsForThisMember.StringDecorations.TryGetValue(Decoration.LinkSDSL, out var linkValue);

                        if (!newStructure)
                        {
                            // If not a new structure, we restart from 0 and add only what's necessary
                            // Note: we made sure to query linkValue before
                            decorationsForThisMember.StringDecorations.Clear();
                            decorationsForThisMember.Decorations.Clear();
                        }

                        // Note: We don't mutate decorationsForThisMember because multiple cbuffer might share the same struct
                        //       We emit OpMemberDecorateString directly on the resulting cbufferStructId

                        // If not specified, add default Link info
                        if (linkValue == null)
                        {
                            var link = $"{TypeName.GetTypeNameWithoutGenerics(cbuffer.ShaderName)}.{member.Name}";
                            if (!compositionPath.IsNullOrEmpty())
                                link = $"{link}.{compositionPath}";

                            context.Add(new OpMemberDecorateString(cbufferStructId, mergedMemberIndex, ParameterizedFlags.DecorationLinkSDSL(link)));
                        }

                        // Also transfer LogicalGroup (from name)
                        if (cbuffer.LogicalGroup != null)
                            context.Add(new OpMemberDecorateString(cbufferStructId, mergedMemberIndex, ParameterizedFlags.DecorationLogicalGroupSDSL(cbuffer.LogicalGroup)));

                        foreach (var stringDecoration in decorationsForThisMember.StringDecorations)
                            context.Add(new OpMemberDecorateString(cbufferStructId, mergedMemberIndex, new ParameterizedFlag<Decoration>(stringDecoration.Key, [.. stringDecoration.Value.AsDisposableLiteralValue().Words])));
                        foreach (var decoration in decorationsForThisMember.Decorations)
                            context.Add(new OpMemberDecorate(cbufferStructId, mergedMemberIndex, new ParameterizedFlag<Decoration>(decoration.Key, decoration.Value)));
                    }
                }
            }

            var idRemapping = new Dictionary<int, int>();
            var removedIds = new HashSet<int>();
            foreach (var cbuffersEntry in cbuffersByNames)
            {
                var cbuffers = cbuffersEntry.ToList();
                var cbuffersSpan = CollectionsMarshal.AsSpan(cbuffers);

                // In all cases, we update name to one without .0 .1 suffix
                // (we do it even for case count == 1 because all buffer except one might have been optimized away)
                context.Names[cbuffersSpan[0].Variable.Data.IdResult.Value] = cbuffersEntry.Key;

                if (cbuffersEntry.Count() == 1)
                {
                    ProcessDecorations(cbuffersSpan, context.Types[cbuffersEntry.First().StructType], false);
                }
                // More than 1 cbuffers with same name
                else
                {
                    int offset = 0;
                    // TODO: Analyze and skip cbuffers parts which are unused
                    foreach (ref var cbuffer in cbuffersSpan)
                    {
                        cbuffer.MemberIndexOffset = offset;
                        offset += cbuffer.StructType.Members.Count;
                    }
                    var variables = cbuffers.ToDictionary(x => x.Variable.Data.IdResult.Value, x => x);
                    var structTypes = cbuffers.Select(x => x.StructType);

                    var mergedCbufferStruct = new ConstantBufferSymbol(cbuffersEntry.Key, structTypes.SelectMany(x => x.Members).ToList());
                    var mergedCbufferStructId = context.DeclareCBuffer(mergedCbufferStruct);
                    var mergedCbufferPtrStruct = new PointerType(mergedCbufferStruct, Specification.StorageClass.Uniform);
                    var mergedCbufferPtrStructId = context.GetOrRegister(mergedCbufferPtrStruct);

                    ProcessDecorations(cbuffersSpan, mergedCbufferStructId, true);

                    // Remap member ids
                    foreach (var i in buffer)
                    {
                        if (i.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain)
                        {
                            if (variables.TryGetValue(accessChain.BaseId, out var cbuffer) && cbuffer.MemberIndexOffset > 0)
                            {
                                // According to spec, this must be a OpConstant (and we only create them with int)
                                var indexes = accessChain.Values.Elements.Span;
                                var constantId = indexes[0];
                                var index = cbuffer.MemberIndexOffset + (int)context.GetConstantValue(constantId);
                                indexes[0] = context.CompileConstant(index).Id;

                                // Regenerate buffer (since we modify accessChain.Values, it doesn't get rebuilt automatically)
                                accessChain.UpdateInstructionMemory();
                            }
                        }
                        // Out of safety, check for any OpLoad/OpStore on the variables (forbidden, only OpAccessChain)
                        else if (i.Op == Op.OpLoad && (OpLoad)i is { } load)
                        {
                            if (variables.TryGetValue(load.Pointer, out var cbuffer))
                                throw new NotSupportedException("Can't OpLoad with cbuffer");
                        }
                        else if (i.Op == Op.OpStore && (OpStore)i is { } store)
                        {
                            if (variables.TryGetValue(store.Pointer, out var cbuffer))
                                throw new NotSupportedException("Can't OpLoad with cbuffer");
                        }
                    }

                    // Update first variable to use new type
                    cbuffersSpan[0].Variable.Data.IdResultType = mergedCbufferPtrStructId;
                    foreach (var i in buffer)
                    {
                        if (i.Op == Op.OpName && (OpName)i is { } name)
                        {
                            // Ensure cbuffer variable name is correct (it might still have a pending number such as Test.0 if there was multiple buffers with same name)
                            if (cbuffersSpan[0].Variable.Data.IdResult == name.Target)
                                name.Name = cbuffersEntry.Key;
                            // Remove any other OpName (after remapping they would all point to the merged variable)
                            foreach (var cbuffer in cbuffersSpan[1..])
                            {
                                if (cbuffer.Variable.Data.IdResult == name.Target)
                                    SetOpNop(i.Data.Memory.Span);
                            }
                        }
                    }

                    foreach (ref var cbuffer in cbuffersSpan.Slice(1))
                    {
                        // Update all cbuffers access to be replaced with first variable (unified cbuffer)
                        idRemapping.Add(cbuffer.Variable.Data.IdResult.Value, cbuffersSpan[0].Variable.Data.IdResult.Value);
                        removedIds.Add(cbuffer.Variable.Data.IdResult.Value);
                        // Remove other cbuffer variables
                        SetOpNop(cbuffer.Variable.Data.Memory.Span);
                        // TODO: Do we want to remove unecessary types?
                        //       Maybe we don't care as they are not used anymore, they will be ignored.
                        //       Also, if we do so, maybe we could do it as part of a global pass at the end rather than now?
                    }
                }
            }

            SpirvBuilder.RemapIds(buffer, 0, buffer.Count, idRemapping);
            SpirvBuilder.RemapIds(context.GetBuffer(), 0, context.GetBuffer().Count, idRemapping);
        }

        private void ComputeCBufferOffsets(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer)
        {
            var cbuffers = buffer
                .Where(x => x.Op == Op.OpVariableSDSL)
                // Note: MemberIndexOffset is simply a shift in Members index, not something like a byte offset
                .Select(x => (
                    Variable: x,
                    StructTypePtrId: x.Data.IdResultType.Value,
                    StructType: context.ReverseTypes[x.Data.IdResultType.Value] is PointerType p && p.StorageClass == Specification.StorageClass.Uniform && p.BaseType is StructuredType s ? s : null,
                    MemberIndexOffset: 0))
                .Where(x => x.StructType != null)
                .ToList();

            EffectTypeDescription ConvertStructType(SpirvContext context, StructType s)
            {
                var structId = context.Types[s];

                var hasOffsetDecorations = false;
                foreach (var i in context)
                {
                    if (i.Op == Op.OpMemberDecorate && (OpMemberDecorate)i is { Decoration: { Value: Decoration.Offset } } memberDecorate && memberDecorate.StructureType == structId)
                    {
                        hasOffsetDecorations = true;
                        break;
                    }
                }

                var members = new EffectTypeMemberDescription[s.Members.Count];
                var offset = 0;
                for (int i = 0; i < s.Members.Count; ++i)
                {
                    var memberSize = SpirvBuilder.ComputeCBufferOffset(s.Members[i].Type, s.Members[i].TypeModifier, ref offset);

                    members[i] = new EffectTypeMemberDescription
                    {
                        Name = s.Members[i].Name,
                        Type = ConvertType(context, s.Members[i].Type, s.Members[i].TypeModifier),
                        Offset = offset,
                    };

                    // Note: we assume if already added by another cbuffer using this type, the offsets were computed the same way
                    if (!hasOffsetDecorations)
                        DecorateMember(context, structId, i, offset, memberSize, s.Members[i].Type, s.Members[i].TypeModifier);

                    offset += memberSize;
                }
                return new EffectTypeDescription { Class = EffectParameterClass.Struct, RowCount = 1, ColumnCount = 1, Name = s.Name, Members = members, ElementSize = offset };
            }


            EffectTypeDescription ConvertType(SpirvContext context, SymbolType symbolType, TypeModifier typeModifier)
            {
                return symbolType switch
                {
                    ScalarType { TypeName: "int" } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Int, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                    ScalarType { TypeName: "float" } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Float, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                    ArrayType a => ConvertArrayType(context, a, typeModifier),
                    StructType s => ConvertStructType(context, s),
                    // TODO: should we use RowCount instead? (need to update Stride)
                    VectorType v => ConvertType(context, v.BaseType, typeModifier) with { Class = EffectParameterClass.Vector, ColumnCount = v.Size },
                    MatrixType m when typeModifier == TypeModifier.ColumnMajor || typeModifier == TypeModifier.None
                        => ConvertType(context, m.BaseType, typeModifier) with { Class = EffectParameterClass.MatrixColumns, RowCount = m.Rows, ColumnCount = m.Columns },
                    MatrixType m when typeModifier == TypeModifier.RowMajor
                        => ConvertType(context, m.BaseType, typeModifier) with { Class = EffectParameterClass.MatrixRows, RowCount = m.Rows, ColumnCount = m.Columns },
                };

                EffectTypeDescription ConvertArrayType(SpirvContext context, ArrayType a, TypeModifier typeModifier)
                {
                    var typeId = context.Types[a];
                    var elementType = ConvertType(context, a.BaseType, typeModifier);

                    var hasStrideDecoration = false;
                    foreach (var i in context)
                    {
                        if (i.Op == Op.OpDecorate && (OpDecorate)i is { Decoration: { Value: Decoration.ArrayStride } } arrayStrideDecoration && arrayStrideDecoration.Target == typeId)
                        {
                            hasStrideDecoration = true;
                        }
                    }

                    if (!hasStrideDecoration)
                    {
                        var elementSize = SpirvBuilder.TypeSizeInBuffer(a.BaseType, typeModifier).Size;
                        var arrayStride = (elementSize + 15) / 16 * 16;
                        context.Add(new OpDecorate(typeId, ParameterizedFlags.DecorationArrayStride(arrayStride)));
                    }

                    return elementType with { Elements = a.Size };
                }
            }

            // Scan LinkSDSL and LogicalGroupSDSL decorations
            Dictionary<(int StructType, int Member), string> links = new();
            Dictionary<(int StructType, int Member), string> logicalGroups = new();
            foreach (var i in context)
            {
                if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.LinkSDSL, Parameters: { } m } } memberDecorate)
                {
                    using var n = new LiteralValue<string>(m.Span);
                    links.Add((memberDecorate.StructType, memberDecorate.Member), n.Value);
                }
                else if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Decoration: { Value: Decoration.LogicalGroupSDSL, Parameters: { } m2 } } memberDecorate2)
                {
                    using var n = new LiteralValue<string>(m2.Span);
                    logicalGroups.Add((memberDecorate2.StructType, memberDecorate2.Member), n.Value);
                }
            }

            foreach (var cbuffer in cbuffers)
            {
                int constantBufferOffset = 0;
                var cb = cbuffer.StructType;
                var structTypeId = context.Types[cb];

                var memberInfos = new EffectValueDescription[cb.Members.Count];
                for (var index = 0; index < cb.Members.Count; index++)
                {
                    // Properly compute size and offset according to DirectX rules
                    var member = cb.Members[index];
                    var memberSize = SpirvBuilder.ComputeCBufferOffset(member.Type, member.TypeModifier, ref constantBufferOffset);

                    DecorateMember(context, structTypeId, index, constantBufferOffset, memberSize, member.Type, member.TypeModifier);

                    if (!links.TryGetValue((structTypeId, index), out var linkName))
                        throw new InvalidOperationException($"Could not find cbuffer member link info; it should have been generated during {MergeCBuffers}");
                    // Allowed to be not set (in which case logicalGroup == null)
                    logicalGroups.TryGetValue((structTypeId, index), out var logicalGroup);

                    memberInfos[index] = new EffectValueDescription
                    {
                        Type = ConvertType(context, member.Type, member.TypeModifier),
                        RawName = member.Name,
                        KeyInfo = new EffectParameterKeyInfo { KeyName = linkName },
                        Offset = constantBufferOffset,
                        Size = memberSize,
                        LogicalGroup = logicalGroup,
                    };

                    // Adjust offset for next item
                    constantBufferOffset += memberSize;
                }

                globalContext.Reflection.ConstantBuffers.Add(new EffectConstantBufferDescription
                {
                    Name = context.Names[cbuffer.Variable.Data.IdResult.Value],
                    // Round buffer size to next multiple of 16 bytes
                    Size = (constantBufferOffset + 15) / 16 * 16,

                    Type = ConstantBufferType.ConstantBuffer,
                    Members = memberInfos,
                });
            }
        }

        private static void DecorateMember(SpirvContext context, int structTypeId, int index, int offset, int size, SymbolType memberType, TypeModifier memberTypeModifier)
        {
            context.Add(new OpMemberDecorate(structTypeId, index, ParameterizedFlags.DecorationOffset(offset)));
            if (memberType is MatrixType or ArrayType { BaseType: MatrixType })
            {
                if (memberTypeModifier != TypeModifier.ColumnMajor)
                    context.Add(new OpMemberDecorate(structTypeId, index, new ParameterizedFlag<Decoration>(Decoration.ColMajor, [])));
                else if (memberTypeModifier != TypeModifier.RowMajor)
                    context.Add(new OpMemberDecorate(structTypeId, index, new ParameterizedFlag<Decoration>(Decoration.RowMajor, [])));
                context.Add(new OpMemberDecorate(structTypeId, index, ParameterizedFlags.DecorationMatrixStride(16)));
            }
        }
    }
}
