using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    private record struct VariableMetadata(string? Link = null, string? ResourceGroup = null, string? LogicalGroup = null, bool Color = false);
    private record struct CBufferMemberMetadata(string? Link = null, string? LogicalGroup = null, bool Color = false, object? DefaultValue = null);

    private Dictionary<int, VariableMetadata> variableMetadata = new();
    // Note: cbuffer might share same struct, which is why we store this info per variable instead of per struct (as per OpMemberDecorate was doing)
    private Dictionary<int, CBufferMemberMetadata[]> cbufferMemberMetadata = new();

    private static bool IsResourceType(SymbolType type)
        => type is TextureType or SamplerType or BufferType or StructuredBufferType or ByteAddressBufferType or ConstantBufferSymbol;

    // Process LinkSDSL, ResourceGroupSDSL and LogicalGroupSDSL; Info will be stored in resourceLinks and cbufferMemberLinks
    private void ProcessLinks(SpirvContext context, SpirvBuffer buffer)
    {
        // Link attribute: postfix with composition path
        string? compositionPath = null;
        string? shaderName = null;

        var variableDecorationMetadata = new Dictionary<int, VariableMetadata>();
        var structDecorationMetadata = new Dictionary<(int, int), CBufferMemberMetadata>();

        foreach (var i in context)
        {
            if (i.Op == Specification.Op.OpDecorate && (OpDecorate)i is
                { Decoration: Specification.Decoration.ColorSDSL } decorate)
            {
                ref var metadata = ref CollectionsMarshal.GetValueRefOrAddDefault(variableDecorationMetadata, decorate.Target, out _);
                switch (decorate.Decoration)
                {
                    case Specification.Decoration.ColorSDSL:
                        metadata.Color = true;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (i.Op == Specification.Op.OpDecorateString && (OpDecorateString)i is
            { Decoration: Specification.Decoration.LinkSDSL or Specification.Decoration.ResourceGroupSDSL or Specification.Decoration.LogicalGroupSDSL } decorateString)
            {
                ref var metadata = ref CollectionsMarshal.GetValueRefOrAddDefault(variableDecorationMetadata, decorateString.Target, out _);
                switch (decorateString.Decoration)
                {
                    case Specification.Decoration.LinkSDSL:
                        metadata.Link = decorateString.Value;
                        break;
                    case Specification.Decoration.ResourceGroupSDSL:
                        metadata.ResourceGroup = decorateString.Value;
                        break;
                    case Specification.Decoration.LogicalGroupSDSL:
                        metadata.LogicalGroup = decorateString.Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (i.Op == Specification.Op.OpMemberDecorate && (OpMemberDecorate)i is
            { Decoration: Specification.Decoration.ColorSDSL } memberDecorate)
            {
                ref var metadata = ref CollectionsMarshal.GetValueRefOrAddDefault(structDecorationMetadata, (memberDecorate.StructureType, memberDecorate.Member), out _);
                switch (memberDecorate.Decoration)
                {
                    case Specification.Decoration.ColorSDSL:
                        metadata.Color = true;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (i.Op == Specification.Op.OpMemberDecorateString && (OpMemberDecorateString)i is
            { Decoration: Specification.Decoration.LinkSDSL } memberDecorateString)
            {
                ref var metadata = ref CollectionsMarshal.GetValueRefOrAddDefault(structDecorationMetadata, (memberDecorateString.StructType, memberDecorateString.Member), out _);
                switch (memberDecorateString.Decoration)
                {
                    case Specification.Decoration.LinkSDSL:
                        metadata.Link = memberDecorateString.Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        // Collect variable infos
        foreach (var i in buffer)
        {
            if (i.Op == Specification.Op.OpCompositionSDSL && (OpCompositionSDSL)i is { } composition)
            {
                compositionPath = composition.CompositionPath;
            }
            else if (i.Op == Specification.Op.OpCompositionEndSDSL)
            {
                compositionPath = null;
                shaderName = null;
            }
            else if (i.Op == Specification.Op.OpShaderSDSL && (OpShaderSDSL)i is { } shader)
            {
                shaderName = shader.ShaderName;
            }
            else if (i.Op == Specification.Op.OpVariableSDSL && (OpVariableSDSL)i is { } variableInstruction)
            {
                if (shaderName is null)
                    throw new InvalidOperationException($"{nameof(OpVariableSDSL)} {context.Names[variableInstruction.ResultId]} without {nameof(OpShaderSDSL)}, can't figure out owner type");

                bool isStage = (variableInstruction.Flags & Specification.VariableFlagsMask.Stage) != 0;
                var variablePointerType = (PointerType)context.ReverseTypes[variableInstruction.ResultType];
                var variableType = variablePointerType.BaseType;

                if (!variableDecorationMetadata.TryGetValue(variableInstruction.ResultId, out var metadata)
                    || metadata.Link == null)
                    metadata.Link = GenerateLinkName(shaderName, context.Names[variableInstruction.ResultId]);

                if (!isStage)
                    metadata.Link = ComposeLinkName(metadata.Link, compositionPath);

                variableMetadata[variableInstruction.ResultId] = metadata;

                // Build per-member metadata for cbuffers: resolve link names (from decorations or generated from shader/member name),
                // compose with composition path for non-stage variables, and propagate the cbuffer's LogicalGroup to each member.
                if (variableType is ConstantBufferSymbol cb)
                {
                    var constantBufferStructId = context.Types[cb];
                    CBufferMemberMetadata[] memberLinks = new CBufferMemberMetadata[cb.Members.Count];
                    for (var index = 0; index < cb.Members.Count; index++)
                    {
                        var member = cb.Members[index];
                        if (!structDecorationMetadata.TryGetValue((constantBufferStructId, index), out var memberLink)
                            || memberLink.Link == null)
                            memberLink.Link = GenerateLinkName(shaderName, member.Name);

                        if (!isStage)
                            memberLink.Link = ComposeLinkName(memberLink.Link, compositionPath);
                        memberLink.LogicalGroup = metadata.LogicalGroup;
                        memberLinks[index] = memberLink;
                    }

                    cbufferMemberMetadata.Add(variableInstruction.ResultId, memberLinks);
                }
            }
        }
    }

    private void RenameVariables(MixinGlobalContext globalContext, SpirvContext context, SpirvBuffer temp)
    {
        // Collect variables by names
        string? compositionPath = null;
        var shaderNameWithComposition = string.Empty;
        Dictionary<int, string> prefixes = new();
        foreach (var i in temp)
        {
            if (i.Op == Specification.Op.OpCompositionSDSL && (OpCompositionSDSL)i is { } composition)
            {
                compositionPath = composition.CompositionPath;
            }
            else if (i.Op == Specification.Op.OpCompositionEndSDSL)
            {
                compositionPath = null;
            }
            else if (i.Op == Specification.Op.OpShaderSDSL && (OpShaderSDSL)i is { } shader)
            {
                shaderNameWithComposition = compositionPath != null
                    ? $"{compositionPath}.{shader.ShaderName}"
                    : shader.ShaderName;
            }
            else if (i.Op == Specification.Op.OpVariableSDSL && (OpVariableSDSL)i is
            { StorageClass: Specification.StorageClass.UniformConstant or Specification.StorageClass.StorageBuffer } variable)
            {
                // Note: we don't rename cbuffer as they have been merged and don't belong to a specific shader/composition anymore
                var type = context.ReverseTypes[variable.ResultType];
                if (type is not ConstantBufferSymbol)
                    prefixes[variable.ResultId] = shaderNameWithComposition;
            }
            else if (i.Op == Specification.Op.OpTypeStruct && (OpTypeStruct)i is { } structType)
            {
                prefixes[structType.ResultId] = shaderNameWithComposition;
            }
            else if (i.Op == Specification.Op.OpFunction && (OpFunction)i is { } function)
            {
                prefixes[function.ResultId] = shaderNameWithComposition;
            }
        }

        // Now, reprocess context with those names
        foreach (var i in context)
        {
            if (i.Op == Specification.Op.OpName && (OpName)i is { } name)
            {
                if (prefixes.TryGetValue(name.Target, out var prefix))
                {
                    var updatedName = $"{prefix}.{name.Name}";
                    name.Name = updatedName;

                    // Now, make sure it's all valid HLSL/GLSL characters (this will replace multiple invalid characters with a single underscore)
                    // Otherwise, EffectReflection RawName won't match
                    updatedName = SpirvBuilder.RemoveInvalidCharactersFromSymbol(updatedName);
                    context.Names[name.Target] = updatedName;
                }
            }
        }
    }

    // Emit reflection (except ConstantBuffers which was emitted during ComputeCBufferReflection)
    private unsafe void ProcessReflection(MixinGlobalContext globalContext, SpirvContext context, SpirvBuffer buffer, Options options)
    {
        Span<int> slotCounts = stackalloc int[options.ResourcesRegisterSeparate ? 4 : 1];
        slotCounts.Clear();

        // If areResourcesSharingSlots is true, every slot type will point to same value
        ref var srvSlot = ref slotCounts[options.ResourcesRegisterSeparate ? 0 : 0];
        ref var samplerSlot = ref slotCounts[options.ResourcesRegisterSeparate ? 1 : 0];
        ref var cbufferSlot = ref slotCounts[options.ResourcesRegisterSeparate ? 2 : 0];
        ref var uavSlot = ref slotCounts[options.ResourcesRegisterSeparate ? 3 : 0];

        // TODO: do this once at root level and reuse for child mixin
        var samplerStates = new Dictionary<int, Graphics.SamplerStateDescription>();
        foreach (var i in context)
        {
            if (i.Op == Specification.Op.OpDecorate &&
                     (OpDecorate)i is { Decoration: Specification.Decoration.SamplerStateSDSL, DecorationParameters: { } p } decorate)
            {
                var s = p.Span;
                samplerStates[decorate.Target] = new Graphics.SamplerStateDescription
                {
                    Filter = (Graphics.TextureFilter)s[0],
                    AddressU = (Graphics.TextureAddressMode)s[1],
                    AddressV = (Graphics.TextureAddressMode)s[2],
                    AddressW = (Graphics.TextureAddressMode)s[3],
                    MipMapLevelOfDetailBias = BitConverter.Int32BitsToSingle(s[4]),
                    MaxAnisotropy = s[5],
                    CompareFunction = (Graphics.CompareFunction)s[6],
                    MinMipLevel = BitConverter.Int32BitsToSingle(s[7]),
                    MaxMipLevel = BitConverter.Int32BitsToSingle(s[8]),
                    BorderColor = new(BitConverter.Int32BitsToSingle(s[9]), BitConverter.Int32BitsToSingle(s[10]),
                                     BitConverter.Int32BitsToSingle(s[11]), BitConverter.Int32BitsToSingle(s[12])),
                };
            }
        }

        string currentShaderName = string.Empty;
        foreach (var i in buffer)
        {
            if (i.Op == Specification.Op.OpShaderSDSL && (OpShaderSDSL)i is { } shader)
            {
                currentShaderName = shader.ShaderName;
            }
            else if (i.Op == Specification.Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable)
            {
                var variablePointerType = (PointerType)context.ReverseTypes[variable.ResultType];
                var variableType = variablePointerType.BaseType;

                if (IsResourceType(variableType))
                {
                    var name = context.Names[variable.ResultId];

                    variableMetadata.TryGetValue(variable.ResultId, out var linkInfo);
                    var linkName = variableType switch
                    {
                        // TODO: Special case, Stride EffectCompiler.CleanupReflection() expect a different format here (let's fix that later in Stride)
                        //       Anyway, since buffer is merged, KeyName with form ShaderName.VariableName doesn't make sense as it doesn't belong to a specific shader anymore
                        ConstantBufferSymbol cb => name,
                        _ => linkInfo.Link ?? throw new InvalidOperationException($"Missing Link info for variable {name}"),
                    };

                    var effectResourceBinding = new EffectResourceBindingDescription
                    {
                        KeyInfo = new EffectParameterKeyInfo { KeyName = linkName },
                        ElementType = default,
                        RawName = name,
                        ResourceGroup = linkInfo.ResourceGroup,
                        //Stage = , // filed by ShaderCompiler
                        LogicalGroup = linkInfo.LogicalGroup,
                    };

                    if (variableType is TextureType or BufferType or StructuredBufferType or ByteAddressBufferType)
                    {
                        bool isUAV = variableType switch
                        {
                            TextureType t1 => t1.Sampled == 2,
                            BufferType b1 => b1.WriteAllowed,
                            StructuredBufferType sb1 => sb1.WriteAllowed,
                            ByteAddressBufferType bab1 => bab1.WriteAllowed,
                            _ => throw new NotSupportedException($"Unsupported variable type: {variableType}"),
                        };
                        ref var slot = ref (isUAV ? ref uavSlot : ref srvSlot);
                        effectResourceBinding.Class = isUAV ? EffectParameterClass.UnorderedAccessView : EffectParameterClass.ShaderResourceView;
                        effectResourceBinding.SlotStart = slot;
                        effectResourceBinding.SlotCount = 1;

                        context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.DescriptorSet, [0]));
                        context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.Binding, [slot]));

                        slot++;

                        if (variableType is TextureType t)
                        {
                            var resolved = effectResourceBinding with
                            {
                                Type = (t, t.Multisampled, t.Sampled == 2) switch
                                {
                                    (Texture1DType, false, false) => EffectParameterType.Texture1D,
                                    (Texture2DType, false, false) => EffectParameterType.Texture2D,
                                    (Texture2DType, true, false) => EffectParameterType.Texture2DMultisampled,
                                    (Texture3DType, false, false) => EffectParameterType.Texture3D,
                                    (TextureCubeType, false, false) => EffectParameterType.TextureCube,
                                    (Texture1DType, false, true) => EffectParameterType.RWTexture1D,
                                    (Texture2DType, false, true) => EffectParameterType.RWTexture2D,
                                    (Texture3DType, false, true) => EffectParameterType.RWTexture3D,
                                    _ => throw new NotSupportedException($"Unsupported texture type combination: {t}"),
                                },
                            };
                            globalContext.Reflection.ResourceBindings.Add(resolved);
                            EmitResourceEntry(globalContext, resolved);
                        }
                        else if (variableType is BufferType bufferType)
                        {
                            var resolved = effectResourceBinding with
                            {
                                Type = bufferType.WriteAllowed ? EffectParameterType.RWBuffer : EffectParameterType.Buffer,
                                ElementType = new EffectTypeDescription { Type = ScalarToEffectParameterType(bufferType.BaseType) },
                            };
                            globalContext.Reflection.ResourceBindings.Add(resolved);
                            EmitResourceEntry(globalContext, resolved);
                        }
                        else if (variableType is StructuredBufferType structuredBufferType)
                        {
                            var resolved = effectResourceBinding with
                            {
                                Type = structuredBufferType.WriteAllowed ? EffectParameterType.RWStructuredBuffer : EffectParameterType.StructuredBuffer,
                            };
                            globalContext.Reflection.ResourceBindings.Add(resolved);
                            EmitResourceEntry(globalContext, resolved);

                            // UserTypeGOOGLE and NonWritable decorations are already added during shader compilation

                            var baseType = structuredBufferType.BaseType;
                            // This will add array stride and offsets decorations
                            EmitTypeDecorationsRecursively(context, baseType, SpirvBuilder.AlignmentRules.StructuredBuffer);
                        }
                        else if (variableType is ByteAddressBufferType byteAddressBufferType)
                        {
                            var resolved = effectResourceBinding with
                            {
                                Type = byteAddressBufferType.WriteAllowed ? EffectParameterType.RWByteAddressBuffer : EffectParameterType.ByteAddressBuffer,
                            };
                            globalContext.Reflection.ResourceBindings.Add(resolved);
                            EmitResourceEntry(globalContext, resolved);

                            // UserTypeGOOGLE and NonWritable decorations are already added during shader compilation
                        }
                    }
                    else if (variableType is SamplerType)
                    {
                        var resolved = effectResourceBinding with
                        {
                            Class = EffectParameterClass.Sampler,
                            Type = EffectParameterType.Sampler,
                            SlotStart = samplerSlot,
                            SlotCount = 1,
                        };
                        globalContext.Reflection.ResourceBindings.Add(resolved);

                        Graphics.SamplerStateDescription? samplerDesc = null;
                        if (samplerStates.TryGetValue(variable.ResultId, out var samplerState))
                        {
                            globalContext.Reflection.SamplerStates.Add(
                                new EffectSamplerStateBinding(linkName, samplerState));
                            samplerDesc = samplerState;
                        }

                        EmitResourceEntry(globalContext, resolved, samplerDesc);

                        context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.DescriptorSet, [0]));
                        context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.Binding, [samplerSlot]));

                        samplerSlot++;
                    }
                    else if (variableType is ConstantBufferSymbol)
                    {
                        var resolved = effectResourceBinding with
                        {
                            Class = EffectParameterClass.ConstantBuffer,
                            Type = EffectParameterType.ConstantBuffer,
                            SlotStart = cbufferSlot,
                            SlotCount = 1,
                            ResourceGroup = name,
                        };
                        globalContext.Reflection.ResourceBindings.Add(resolved);
                        // cbuffer entry is emitted first in the group (before textures/samplers)
                        EmitResourceEntry(globalContext, resolved);

                        context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.DescriptorSet, [0]));
                        context.Add(new OpDecorate(variable.ResultId, Specification.Decoration.Binding, [cbufferSlot]));

                        cbufferSlot++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Emits an <see cref="EffectResourceEntry"/> into the appropriate <see cref="EffectResourceGroupDescription"/>.
    /// </summary>
    private static void EmitResourceEntry(MixinGlobalContext globalContext, in EffectResourceBindingDescription binding, Graphics.SamplerStateDescription? samplerState = null)
    {
        var groupName = binding.ResourceGroup ?? "Globals";
        var group = globalContext.Reflection.GetOrCreateGroup(groupName);
        group.Entries.Add(new EffectResourceEntry(binding, samplerState));
    }

    private static EffectParameterType ScalarToEffectParameterType(ScalarType scalarType) => scalarType.Type switch
    {
        Scalar.Float => EffectParameterType.Float,
        Scalar.Half => EffectParameterType.Float,
        Scalar.Double => EffectParameterType.Double,
        Scalar.Int => EffectParameterType.Int,
        Scalar.UInt => EffectParameterType.UInt,
        Scalar.Boolean => EffectParameterType.Bool,
        _ => EffectParameterType.Void,
    };
}
