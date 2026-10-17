// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using Stride.Core.Diagnostics;
using Stride.Shaders.Parsing;
using Stride.Shaders.Spirv.Tools;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// Makes the texture offsets constant. A class compiles a texture offset that is not a constant yet (a helper's parameter,
    /// an <c>[unroll]</c> loop's counter) as an <c>Offset</c> operand, which Vulkan only allows on gathers. As in DXC, the
    /// module is then legalized: helpers inlined, <c>[unroll]</c> loops unrolled, constants propagated; each offset that became
    /// a constant is a <c>ConstOffset</c>, and one that didn't is error SDSL0189.
    /// </summary>
    private static bool MakeSampleOffsetsConstant(ref Span<byte> bytecode, ILogger log)
    {
        var words = MemoryMarshal.Cast<byte, uint>(bytecode).ToArray();
        if (PromoteConstantOffsets(words).VariableOffsets.Count == 0)
            return true;

        // Universal rules: Vulkan's would reject the Offset operand in the input
        var legalized = SpirvTools.LegalizeForHlsl(words, SpirvTools.TargetEnv.Universal_1_6);
        legalized = SpirvTools.Optimize(legalized, PropagateConstantsPasses, preserveInterface: true, SpirvTools.TargetEnv.Universal_1_6);
        var (variableOffsets, needsGatherExtended) = PromoteConstantOffsets(legalized);
        foreach (var location in variableOffsets)
            log.Error($"{location}{SDSLErrorMessages.SDSL0189}");
        if (variableOffsets.Count > 0)
            return false;

        // The Offset operands needed ImageGatherExtended, which Vulkan only allows with its device feature
        if (!needsGatherExtended)
            legalized = RemoveCapability(legalized, Capability.ImageGatherExtended);

        bytecode = MemoryMarshal.AsBytes(legalized.AsSpan());
        return true;
    }

    // After the legalization, an unrolled loop still reads its elements from a local copy of the array: DXC's -O passes are
    // what make them constants (at -O0, DXC gives the same invalid Offset)
    private static readonly string[] PropagateConstantsPasses =
    [
        "--convert-local-access-chains",
        "--eliminate-local-single-store",
        "--eliminate-local-single-block",
        "--scalar-replacement=0",
        "--ccp",
        "--simplify-instructions",
        "--eliminate-dead-code-aggressive",
    ];

    private static uint[] RemoveCapability(uint[] words, Capability capability)
    {
        var result = new List<uint>(words.Length);
        result.AddRange(words.AsSpan(0, 5));
        for (var w = 5; w < words.Length; w += (int)(words[w] >> 16))
        {
            var wordCount = (int)(words[w] >> 16);
            if ((Op)(words[w] & 0xFFFF) != Op.OpCapability || words[w + 1] != (uint)capability)
                result.AddRange(words.AsSpan(w, wordCount));
        }
        return [.. result];
    }

    /// <summary>
    /// Turns each <c>Offset</c> image operand whose value is a constant into a <c>ConstOffset</c>.
    /// </summary>
    /// <returns>The source locations ("file(line,col): ", or empty) of the image operations whose offset is not a constant, and
    /// whether an operand that needs ImageGatherExtended is left (a gather's Offset or ConstOffsets).</returns>
    private static (List<string> VariableOffsets, bool NeedsGatherExtended) PromoteConstantOffsets(uint[] words)
    {
        var needsGatherExtended = false;
        var sourceFiles = new Dictionary<uint, string>();
        var constants = new HashSet<uint>();
        var variableOffsets = new List<string>();
        var location = string.Empty;

        for (var w = 5; w < words.Length; w += (int)(words[w] >> 16))
        {
            var op = (Op)(words[w] & 0xFFFF);
            var wordCount = (int)(words[w] >> 16);
            switch (op)
            {
                case Op.OpString:
                    var text = MemoryMarshal.AsBytes(words.AsSpan(w + 2, wordCount - 2));
                    sourceFiles[words[w + 1]] = Encoding.UTF8.GetString(text[..text.IndexOf((byte)0)]);
                    break;
                case Op.OpConstant or Op.OpConstantComposite or Op.OpConstantNull:
                    constants.Add(words[w + 2]);
                    break;
                case Op.OpLine:
                    location = sourceFiles.TryGetValue(words[w + 1], out var file) ? $"{file}({words[w + 2]},{words[w + 3]}): " : string.Empty;
                    break;
                case Op.OpNoLine or Op.OpFunction:
                    location = string.Empty;
                    break;
            }

            // Gathers take an Offset operand anywhere
            if (op is Op.OpImageGather or Op.OpImageDrefGather or Op.OpImageSparseGather or Op.OpImageSparseDrefGather)
            {
                needsGatherExtended |= wordCount > 6 && (words[w + 6] & (uint)(ImageOperandsMask.Offset | ImageOperandsMask.ConstOffsets)) != 0;
                continue;
            }

            var maskIndex = op switch
            {
                Op.OpImageSampleImplicitLod or Op.OpImageSampleExplicitLod or Op.OpImageSampleProjImplicitLod or Op.OpImageSampleProjExplicitLod
                    or Op.OpImageFetch or Op.OpImageRead or Op.OpImageSparseSampleImplicitLod or Op.OpImageSparseSampleExplicitLod
                    or Op.OpImageSparseSampleProjImplicitLod or Op.OpImageSparseSampleProjExplicitLod or Op.OpImageSparseFetch or Op.OpImageSparseRead => 5,
                Op.OpImageSampleDrefImplicitLod or Op.OpImageSampleDrefExplicitLod or Op.OpImageSampleProjDrefImplicitLod or Op.OpImageSampleProjDrefExplicitLod
                    or Op.OpImageSparseSampleDrefImplicitLod or Op.OpImageSparseSampleDrefExplicitLod
                    or Op.OpImageSparseSampleProjDrefImplicitLod or Op.OpImageSparseSampleProjDrefExplicitLod => 6,
                Op.OpImageWrite => 4,
                _ => -1,
            };
            if (maskIndex == -1 || wordCount <= maskIndex || (words[w + maskIndex] & (uint)ImageOperandsMask.Offset) == 0)
                continue;

            // The operands come in the order of their bits: Bias, Lod, Grad (two), ConstOffset, then Offset
            var mask = (ImageOperandsMask)words[w + maskIndex];
            var offsetIndex = w + maskIndex + 1
                + (mask.HasFlag(ImageOperandsMask.Bias) ? 1 : 0)
                + (mask.HasFlag(ImageOperandsMask.Lod) ? 1 : 0)
                + (mask.HasFlag(ImageOperandsMask.Grad) ? 2 : 0)
                + (mask.HasFlag(ImageOperandsMask.ConstOffset) ? 1 : 0);
            if (constants.Contains(words[offsetIndex]))
                words[w + maskIndex] = (uint)(mask & ~ImageOperandsMask.Offset | ImageOperandsMask.ConstOffset);
            else
                variableOffsets.Add(location);
        }

        return (variableOffsets, needsGatherExtended);
    }
}
