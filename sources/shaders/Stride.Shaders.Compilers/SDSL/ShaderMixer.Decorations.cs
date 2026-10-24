using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    private Dictionary<int, (SpirvBuilder.AlignmentRules Rules, int Size, int[] Offsets)> decoratedStructs = new();
    private Dictionary<int, (SpirvBuilder.AlignmentRules Rules, int Stride)> decoratedArrays = new();

    private void EmitTypeDecorationsRecursively(SpirvContext context, SymbolType symbolType, SpirvBuilder.AlignmentRules alignmentRules, TypeModifier typeModifier = TypeModifier.None)
    {
        switch (symbolType)
        {
            case ArrayType a:
                EmitArrayStrideDecorations(context, a, typeModifier, alignmentRules, out _);
                EmitTypeDecorationsRecursively(context, a.BaseType, alignmentRules, typeModifier);
                break;
            case StructType s:
                EmitStructDecorations(context, s, alignmentRules, out _, out _);
                foreach (var member in s.Members)
                    EmitTypeDecorationsRecursively(context, member.Type, alignmentRules, member.TypeModifier);
                break;
            case VectorType:
            case MatrixType:
            case ScalarType:
                break;
            default:
                throw new NotImplementedException($"Type {symbolType} not implemented");
        }

        ;
    }

    private void EmitArrayStrideDecorations(SpirvContext context, ArrayType a, TypeModifier typeModifier, SpirvBuilder.AlignmentRules alignmentRules, out int arrayStride)
    {
        var typeId = context.Types[a];
        arrayStride = alignmentRules switch
        {
            SpirvBuilder.AlignmentRules.CBuffer => (SpirvBuilder.TypeSizeInBuffer(a.BaseType, typeModifier, alignmentRules).Size + 15) / 16 * 16,
            SpirvBuilder.AlignmentRules.StructuredBuffer or SpirvBuilder.AlignmentRules.StructuredBufferTight => SpirvBuilder.StorageBufferArrayStride(a.BaseType, typeModifier, alignmentRules),
            _ => throw new NotSupportedException($"Unsupported alignment rules: {alignmentRules}"),
        };

        // A type used with two rules is fine as long as they lay it out the same (std430 and the tight layout often do)
        if (decoratedArrays.TryGetValue(typeId, out var arrayDecoration))
        {
            if (arrayDecoration.Stride != arrayStride)
                throw new InvalidOperationException($"Using type {a.ToId()} with both {alignmentRules} and {arrayDecoration.Rules} rules");
            return;
        }

        context.Add(new OpDecorate(typeId, Specification.Decoration.ArrayStride, [arrayStride]));

        decoratedArrays[typeId] = (alignmentRules, arrayStride);
    }

    private void EmitStructDecorations(SpirvContext context, StructType s, SpirvBuilder.AlignmentRules alignmentRules, out int size, out int[] offsets)
    {
        var structId = context.Types[s];
        var offset = 0;
        offsets = new int[s.Members.Count];
        var memberSizes = new int[s.Members.Count];
        for (int i = 0; i < s.Members.Count; ++i)
        {
            memberSizes[i] = SpirvBuilder.ComputeBufferOffset(s.Members[i].Type, s.Members[i].TypeModifier, ref offset, alignmentRules).Size;
            offsets[i] = offset;
            offset += memberSizes[i];
            SpirvBuilder.PadOffsetAfterArray(s.Members[i].Type, s.Members[i].TypeModifier, offsets[i], ref offset, alignmentRules);
        }
        size = offset;

        // A type used with two rules is fine as long as they lay it out the same (std430 and the tight layout often do)
        if (decoratedStructs.TryGetValue(structId, out var structDecoration))
        {
            if (structDecoration.Size != size || !structDecoration.Offsets.AsSpan().SequenceEqual(offsets))
                throw new InvalidOperationException($"Using type {s.ToId()} with both {alignmentRules} and {structDecoration.Rules} rules");
            return;
        }

        for (int i = 0; i < s.Members.Count; ++i)
            DecorateMember(context, structId, i, offsets[i], s.Members[i].Type, s.Members[i].TypeModifier, alignmentRules);

        decoratedStructs[structId] = (alignmentRules, size, offsets);
    }

    /// <summary>
    /// The element stride of a structured buffer, and a matrix element's stride, were written with std430 when the class compiled
    /// (<c>SpirvContext.RegisterStructuredBufferType</c>): rewrites them for the tight layout.
    /// </summary>
    private static void UseTightLayout(SpirvContext context, StructuredBufferType structuredBufferType)
    {
        const SpirvBuilder.AlignmentRules tight = SpirvBuilder.AlignmentRules.StructuredBufferTight;
        var blockId = context.Types[structuredBufferType];
        var runtimeArrayId = -1;
        foreach (var i in context)
        {
            if (i.Op == Op.OpTypeStruct && (OpTypeStruct)i is { } typeStruct && typeStruct.ResultId == blockId)
                runtimeArrayId = typeStruct.MemberTypes.Words[0];
        }

        foreach (var i in context)
        {
            if (i.Op == Op.OpDecorate && (OpDecorate)i is { Decoration: Decoration.ArrayStride } decorate && decorate.Target == runtimeArrayId)
                i.Buffer.Replace(i.Index, new OpDecorate(runtimeArrayId, Decoration.ArrayStride, [SpirvBuilder.StorageBufferArrayStride(structuredBufferType.BaseType, TypeModifier.None, tight)]));
            else if (i.Op == Op.OpMemberDecorate && (OpMemberDecorate)i is { Decoration: Decoration.MatrixStride } memberDecorate && memberDecorate.StructureType == blockId
                && structuredBufferType.BaseType is MatrixType m)
                i.Buffer.Replace(i.Index, new OpMemberDecorate(blockId, 0, Decoration.MatrixStride, [SpirvBuilder.MatrixStride(m, TypeModifier.None, tight)]));
        }
    }
}
