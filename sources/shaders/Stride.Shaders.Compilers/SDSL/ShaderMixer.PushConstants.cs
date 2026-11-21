// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

partial class ShaderMixer
{
    /// <summary>
    /// The constant buffer register of the push-constant block on Direct3D 11 (<c>b13</c>, the last of the 14 slots), where
    /// SpirvTranslator puts it. The other cbuffers get registers from <c>b0</c>, so an effect with a push block has at most 13.
    /// </summary>
    public const int PushConstantsRegisterDirect3D11 = 13;

    /// <summary>
    /// A <c>[PushConstant] cbuffer</c> joined into the effect's push-constant block, in block order.
    /// </summary>
    private record struct PushConstantsPart(string ShaderName, string CBufferName, int MemberCount);

    /// <summary>
    /// Lays out the effect's push-constant block, fills <see cref="EffectReflection.PushConstants"/> and turns the block into
    /// a <see cref="StorageClass.PushConstant"/> variable (no descriptor set, no binding).
    /// </summary>
    /// <remarks>
    /// The layout is tight: each member starts where the previous one ends, and a vector must already be at a multiple of
    /// its size (SDSL0134). Then std430 (Vulkan), cbuffer packing (Direct3D 11) and root constants (Direct3D 12) give the
    /// same offsets, and they are the offsets of the C# struct given to <c>CommandList.SetPushConstants</c>. A block larger
    /// than <see cref="EffectReflection.PushConstantsSize"/> is SDSL0131.
    /// </remarks>
    private bool ProcessPushConstants(MixinGlobalContext globalContext, SpirvContext context, SpirvBuffer buffer, int variableId, List<PushConstantsPart> parts, ILogger log)
    {
        var variableIndex = -1;
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpVariableSDSL && i.Data.IdResult == variableId)
            {
                variableIndex = i.Index;
                break;
            }
        }
        if (variableIndex == -1)
            throw new InvalidOperationException($"Could not find the push-constant variable {variableId}");
        var variable = (OpVariableSDSL)buffer[variableIndex];

        var block = (ConstantBufferSymbol)((PointerType)context.ReverseTypes[variable.ResultType]).BaseType;
        var structTypeId = context.Types[block];
        cbufferMemberMetadata.TryGetValue(variableId, out var metadata);

        var ok = true;
        var offset = 0;
        var members = new EffectValueDescription[block.Members.Count];
        var partIndex = 0;
        var partEnd = parts[0].MemberCount;
        for (var index = 0; index < block.Members.Count; index++)
        {
            while (index >= partEnd)
                partEnd += parts[++partIndex].MemberCount;
            var member = block.Members[index];

            // SDSL0129 already allows only 32-bit scalars and 2- or 4-component vectors of them
            var (size, alignment) = member.Type switch
            {
                ScalarType => (4, 4),
                VectorType vector => (4 * vector.Size, 4 * vector.Size),
                _ => throw new InvalidOperationException($"Unexpected push-constant member type {member.Type}"),
            };
            if (offset % alignment != 0)
            {
                log.Error(string.Format(SDSLErrorMessages.SDSL0134, parts[partIndex].ShaderName, parts[partIndex].CBufferName, member.Name, member.Type, offset, alignment));
                ok = false;
            }

            DecorateMember(context, structTypeId, index, offset, member.Type, member.TypeModifier, SpirvBuilder.AlignmentRules.CBuffer);
            members[index] = new EffectValueDescription
            {
                Type = ConvertType(context, member.Type, member.TypeModifier, SpirvBuilder.AlignmentRules.CBuffer),
                RawName = member.Name,
                KeyInfo = new EffectParameterKeyInfo { KeyName = metadata?[index].Link },
                Offset = offset,
                Size = size,
            };
            offset += size;
        }

        if (offset > EffectReflection.PushConstantsSize)
        {
            var partSizes = new List<string>();
            var memberIndex = 0;
            foreach (var part in parts)
            {
                var partSize = members.AsSpan(memberIndex, part.MemberCount).ToArray().Sum(x => x.Size);
                partSizes.Add($"{part.ShaderName}.{part.CBufferName} {partSize} bytes");
                memberIndex += part.MemberCount;
            }
            log.Error(string.Format(SDSLErrorMessages.SDSL0131, offset, EffectReflection.PushConstantsSize, string.Join(", ", partSizes)));
            ok = false;
        }
        if (!ok)
            return false;

        globalContext.Reflection.PushConstants = new EffectConstantBufferDescription
        {
            Name = PushConstantsName,
            Size = offset,
            Type = ConstantBufferType.PushConstant,
            Members = members,
        };

        // Uniform → PushConstant: the variable, and every access chain into it (an access chain has the storage class of its base)
        variable.StorageClass = StorageClass.PushConstant;
        variable.ResultType = context.GetOrRegister(new PointerType(block, StorageClass.PushConstant));
        var pointers = new HashSet<int> { variableId };
        foreach (var i in buffer)
        {
            if (i.Op == Op.OpAccessChain && (OpAccessChain)i is { } accessChain && pointers.Contains(accessChain.BaseId))
            {
                var resultType = (PointerType)context.ReverseTypes[accessChain.ResultType];
                accessChain.ResultType = context.GetOrRegister(new PointerType(resultType.BaseType, StorageClass.PushConstant));
                pointers.Add(accessChain.ResultId);
            }
        }
        return true;
    }
}
