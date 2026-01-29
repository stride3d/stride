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

namespace Stride.Shaders.Compilers.SDSL
{
    partial class ShaderMixer
    {
        private void GenerateDefaultCBuffer(MixinNode rootMixin, MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer temp)
        {
            var members = new List<StructuredTypeMember>();
            // Remap from variable ID to member index in our new struct
            var variables = new List<int>();
            var variableToMemberIndices = new Dictionary<int, int>();
            // Collect any variable not a stream, not static and not a block
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
                    variables.Add(variable.ResultId);
                    SetOpNop(i.Data.Memory.Span);
                }
            }

            // No global members? Let's finish now
            if (members.Count == 0)
                return;

            var globalCBufferType = new ConstantBufferSymbol("Globals", members);
            var globalCBufferTypeId = context.DeclareCBuffer(globalCBufferType);
            // Transfer metadata from variable to cbuffer member
            var memberMetadata = new CBufferMemberMetadata[members.Count];
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                context.AddMemberName(globalCBufferTypeId, index, member.Name);

                var metadata = variableMetadata[variables[index]];
                memberMetadata[index] = new(Link: metadata.Link, LogicalGroup: metadata.LogicalGroup, Color: metadata.Color);
            }

            // Note: we make sure to add at a previous variable index, otherwise the OpVariableSDSL won't be inside the root MixinNode.StartInstruction/EndInstruction
            temp.FluentReplace(firstVariableIndex, new OpVariableSDSL(context.GetOrRegister(new PointerType(globalCBufferType, Specification.StorageClass.Uniform)), context.Bound++, Specification.StorageClass.Uniform, VariableFlagsMask.Stage, null), out var cbufferVariable);
            context.AddName(cbufferVariable.ResultId, "Globals");
            
            // Update cbuffer links
            cbufferMemberMetadata[cbufferVariable.ResultId] = memberMetadata;
            
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
            
            // Remap decorations and remove OpName
            foreach (var i in context)
            {
                if (i.Op == Op.OpDecorate && (OpDecorate)i is {} decorate)
                {
                    if (variableToMemberIndices.TryGetValue(decorate.Target, out var memberIndex))
                        i.Buffer.Replace(i.Index, new OpMemberDecorate(globalCBufferTypeId, memberIndex, decorate.Decoration, decorate.DecorationParameters));
                }
                else if (i.Op == Op.OpDecorateString && (OpDecorateString)i is {} decorateString)
                {
                    if (variableToMemberIndices.TryGetValue(decorateString.Target, out var memberIndex))
                        i.Buffer.Replace(i.Index, new OpMemberDecorateString(globalCBufferTypeId, memberIndex, decorateString.Decoration, decorateString.Value));
                }
                else if (i.Op == Op.OpName && (OpName)i is { } name)
                {
                    if (variableToMemberIndices.ContainsKey(name.Target))
                        SetOpNop(i.Data.Memory.Span);
                }
            }
        }

        private void MergeCBuffers(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer)
        {
            // Collect Decorations
            Dictionary<(int StructType, int Member), (Dictionary<Decoration, string> StringDecorations, Dictionary<Decoration, MemoryOwner<int>> Decorations)> decorations = new();
            foreach (var i in context)
            {
                if (i.Op == Op.OpMemberDecorateString && (OpMemberDecorateString)i is { Value: string m } memberDecorate)
                {
                    if (!decorations.TryGetValue((memberDecorate.StructType, memberDecorate.Member), out var decorationsForThisMember))
                        decorations.Add((memberDecorate.StructType, memberDecorate.Member), decorationsForThisMember = new([], []));
                    decorationsForThisMember.StringDecorations.Add(memberDecorate.Decoration, m);
                }
                else if (i.Op == Op.OpMemberDecorate && (OpMemberDecorate)i is { DecorationParameters: var m2 } memberDecorate2)
                {
                    if (!decorations.TryGetValue((memberDecorate2.StructureType, memberDecorate2.Member), out var decorationsForThisMember))
                        decorations.Add((memberDecorate2.StructureType, memberDecorate2.Member), decorationsForThisMember = new([], []));
                    decorationsForThisMember.Decorations.Add(memberDecorate2.Decoration, m2.Words);
                }
            }

            string? GetCBufferLogicalGroup(int variableId)
            {
                variableMetadata.TryGetValue(variableId, out var linkName);
                return linkName.LogicalGroup;
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
                    StructType: context.ReverseTypes[x.Variable.Data.IdResultType.Value] is PointerType p && p.StorageClass == Specification.StorageClass.Uniform && p.BaseType is ConstantBufferSymbol s ? s : null,
                    MemberIndexOffset: 0,
                    LogicalGroup: GetCBufferLogicalGroup(x.Variable.Data.IdResult.Value)))
                // TODO: Check Decoration.Block?
                .Where(x => x.StructType != null)
                .GroupBy(x => ShaderClass.GetCBufferRealName(context.Names[x.Variable.Data.IdResult.Value]));

            // This helper method will transfer decorations from the old structure to the new merged structure
            // Also, it will add a default "Link" decoration if none was set
            void ProcessDecorations(Span<(OpDataIndex Variable, string CompositionPath, string ShaderName, int StructTypePtrId, ConstantBufferSymbol? StructType, int MemberIndexOffset, string LogicalGroup)> cbuffersSpan, ConstantBufferSymbol cbufferStruct, bool newStructure)
            {
                var cbufferStructId = context.Types[cbufferStruct];
                int mergedMemberIndex = 0;
                foreach (ref var cbuffer in cbuffersSpan)
                {
                    for (int memberIndex = 0; memberIndex < cbuffer.StructType.Members.Count; memberIndex++, mergedMemberIndex++)
                    {
                        if (!decorations.TryGetValue((context.Types[cbuffer.StructType], memberIndex), out var decorationsForThisMember))
                            decorations.Add((context.Types[cbuffer.StructType], memberIndex), decorationsForThisMember = new(new(), new()));

                        if (newStructure)
                        {
                            // Transfer previous decorations
                            foreach (var stringDecoration in decorationsForThisMember.StringDecorations)
                                context.Add(new OpMemberDecorateString(cbufferStructId, mergedMemberIndex, stringDecoration.Key, stringDecoration.Value));
                            foreach (var decoration in decorationsForThisMember.Decorations)
                                context.Add(new OpMemberDecorate(cbufferStructId, mergedMemberIndex, decoration.Key, [..decoration.Value.Span]));
                        }
                    }
                }
            }

            // Transfer cbufferMemberLinks to new structure
            CBufferMemberMetadata[] GenerateCBufferLinks(int cbufferVariableId, Span<(OpDataIndex Variable, string CompositionPath, string ShaderName, int StructTypePtrId, ConstantBufferSymbol? StructType, int MemberIndexOffset, string LogicalGroup)> cbuffersSpan, ConstantBufferSymbol cbufferStruct)
            {
                int mergedMemberIndex = 0;
                var links = new CBufferMemberMetadata[cbufferStruct.Members.Count];
                foreach (ref var cbuffer in cbuffersSpan)
                {
                    for (int memberIndex = 0; memberIndex < cbuffer.StructType.Members.Count; memberIndex++, mergedMemberIndex++)
                    {
                        links[mergedMemberIndex] = cbufferMemberMetadata[cbuffer.Variable.Data.IdResult.Value][memberIndex];
                    }
                }

                return links;
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
                    ProcessDecorations(cbuffersSpan, cbuffersEntry.First().StructType, false);
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

                    ProcessDecorations(cbuffersSpan, mergedCbufferStruct, true);
                    
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
                    cbufferMemberMetadata[cbuffersSpan[0].Variable.Data.IdResult.Value] = GenerateCBufferLinks(cbuffersSpan[0].Variable.Data.IdResult.Value, cbuffersSpan, mergedCbufferStruct);
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

        EffectTypeDescription ConvertStructType(SpirvContext context, StructType s, SpirvBuilder.AlignmentRules alignmentRules)
        {
            EmitStructDecorations(context, s, alignmentRules, out int size, out var offsets);
            
            var members = new EffectTypeMemberDescription[s.Members.Count];
            for (int i = 0; i < s.Members.Count; ++i)
            {
                members[i] = new EffectTypeMemberDescription
                {
                    Name = s.Members[i].Name,
                    Type = ConvertType(context, s.Members[i].Type, s.Members[i].TypeModifier, alignmentRules),
                    Offset = offsets[i],
                };
            }
            return new EffectTypeDescription { Class = EffectParameterClass.Struct, RowCount = 1, ColumnCount = 1, Name = s.Name, Members = members, ElementSize = size };
        }

        EffectTypeDescription ConvertType(SpirvContext context, SymbolType symbolType, TypeModifier typeModifier, SpirvBuilder.AlignmentRules alignmentRules)
        {
            return symbolType switch
            {
                ScalarType { Type: Scalar.Boolean } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Bool, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                ScalarType { Type: Scalar.UInt } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.UInt, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                ScalarType { Type: Scalar.Int } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Int, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                ScalarType { Type: Scalar.Float } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Float, RowCount = 1, ColumnCount = 1, ElementSize = 4 },
                ScalarType { Type: Scalar.Double } => new EffectTypeDescription { Class = EffectParameterClass.Scalar, Type = EffectParameterType.Double, RowCount = 1, ColumnCount = 1, ElementSize = 8 },
                ArrayType a => ConvertArrayType(context, a, typeModifier, alignmentRules),
                StructType s => ConvertStructType(context, s, alignmentRules),
                // TODO: should we use RowCount instead? (need to update Stride)
                VectorType v => ConvertType(context, v.BaseType, typeModifier, alignmentRules) with { Class = EffectParameterClass.Vector, RowCount = 1, ColumnCount = v.Size },
                // Note: this is HLSL-style so Rows/Columns meaning is swapped
                //       however, for type/class, both TypeModifier and EffectParameterType are following HLSL
                MatrixType m when typeModifier == TypeModifier.ColumnMajor || typeModifier == TypeModifier.None
                    => ConvertType(context, m.BaseType, typeModifier, alignmentRules) with { Class = EffectParameterClass.MatrixColumns, RowCount = m.Columns, ColumnCount = m.Rows },
                MatrixType m when typeModifier == TypeModifier.RowMajor
                    => ConvertType(context, m.BaseType, typeModifier, alignmentRules) with { Class = EffectParameterClass.MatrixRows, RowCount = m.Columns, ColumnCount = m.Rows },
            };

            EffectTypeDescription ConvertArrayType(SpirvContext context, ArrayType a, TypeModifier typeModifier, SpirvBuilder.AlignmentRules alignmentRules)
            {
                EmitArrayStrideDecorations(context, a, typeModifier, alignmentRules, out var arrayStride);

                var elementType = ConvertType(context, a.BaseType, typeModifier, alignmentRules);
                return elementType with { Elements = a.Size };
            }
        }

        private void ComputeCBufferReflection(MixinGlobalContext globalContext, SpirvContext context, NewSpirvBuffer buffer)
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

            foreach (var cbuffer in cbuffers)
            {
                int constantBufferOffset = 0;
                var cb = cbuffer.StructType;
                var structTypeId = context.Types[cb];

                var memberInfos = new EffectValueDescription[cb.Members.Count];
                if (!cbufferMemberMetadata.TryGetValue(cbuffer.Variable.Data.IdResult.Value, out var cbufferMetadata))
                    throw new InvalidOperationException($"Could not find cbuffer member link info for {context.Names[cbuffer.Variable.Data.IdResult.Value]}; it should have been generated during {MergeCBuffers}");
                
                for (var index = 0; index < cb.Members.Count; index++)
                {
                    // Properly compute size and offset according to DirectX rules
                    var member = cb.Members[index];
                    var memberSize = SpirvBuilder.ComputeBufferOffset(member.Type, member.TypeModifier, ref constantBufferOffset, SpirvBuilder.AlignmentRules.CBuffer).Size;

                    DecorateMember(context, structTypeId, index, constantBufferOffset, memberSize, member.Type, member.TypeModifier);

                    var metadata = cbufferMetadata[index];

                    memberInfos[index] = new EffectValueDescription
                    {
                        Type = ConvertType(context, member.Type, member.TypeModifier, SpirvBuilder.AlignmentRules.CBuffer),
                        RawName = member.Name,
                        KeyInfo = new EffectParameterKeyInfo { KeyName = metadata.Link },
                        Offset = constantBufferOffset,
                        Size = memberSize,
                        LogicalGroup = metadata.LogicalGroup,
                    };
                    if (metadata.Color)
                    {
                        if (member.Type is not VectorType { BaseType: { Type: Scalar.Float }, Size: 3 or 4 })
                            throw new InvalidOperationException("[Color] attribute can only be applied on float3/float4 vector types");
                        memberInfos[index].Type.Class = EffectParameterClass.Color;
                    }

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
            context.Add(new OpMemberDecorate(structTypeId, index, Decoration.Offset, [offset]));
            if (memberType is MatrixType or ArrayType { BaseType: MatrixType })
            {
                // HLSL row_major    => SPIR-V ColMajor
                // HLSL column_major => SPIR-V RowMajor
                // HLSL nothing      => SPIR-V RowMajor
                if (memberTypeModifier == TypeModifier.RowMajor)
                    context.Add(new OpMemberDecorate(structTypeId, index, Decoration.ColMajor, []));
                else
                    context.Add(new OpMemberDecorate(structTypeId, index, Decoration.RowMajor, []));
                context.Add(new OpMemberDecorate(structTypeId, index, Decoration.MatrixStride, [16]));
            }
        }
    }
}
