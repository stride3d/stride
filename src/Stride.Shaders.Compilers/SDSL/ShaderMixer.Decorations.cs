using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

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
        if (decoratedArrays.TryGetValue(typeId, out var arrayDecoration))
        {
            if (arrayDecoration.Rules != alignmentRules)
                throw new InvalidOperationException($"Using type {a.ToId()} with both {alignmentRules} and {arrayDecoration.Rules} rules");

            arrayStride = arrayDecoration.Stride;
            return;
        }

        var elementSize = SpirvBuilder.TypeSizeInBuffer(a.BaseType, typeModifier, alignmentRules).Size;
        arrayStride = alignmentRules switch
        {
            SpirvBuilder.AlignmentRules.CBuffer => (elementSize + 15) / 16 * 16,
            SpirvBuilder.AlignmentRules.StructuredBuffer => elementSize,
        };
        context.Add(new OpDecorate(typeId, Specification.Decoration.ArrayStride, [arrayStride]));
    }

    private void EmitStructDecorations(SpirvContext context, StructType s, SpirvBuilder.AlignmentRules alignmentRules, out int size, out int[] offsets)
    {
        var structId = context.Types[s];
        if (decoratedStructs.TryGetValue(structId, out var structDecoration))
        {
            if (structDecoration.Rules != alignmentRules)
                throw new InvalidOperationException($"Using type {s.ToId()} with both {alignmentRules} and {structDecoration.Rules} rules");
            offsets = structDecoration.Offsets;
            size = structDecoration.Size;
            return;
        }

        var offset = 0;
        offsets = new int[s.Members.Count];
        for (int i = 0; i < s.Members.Count; ++i)
        {
            var memberSize = SpirvBuilder.ComputeBufferOffset(s.Members[i].Type, s.Members[i].TypeModifier, ref offset, alignmentRules).Size;

            // Note: we assume if already added by another cbuffer using this type, the offsets were computed the same way
            offsets[i] = offset;
            DecorateMember(context, structId, i, offset, memberSize, s.Members[i].Type, s.Members[i].TypeModifier);

            offset += memberSize;
        }

        decoratedStructs[structId] = (alignmentRules, offset, offsets);
        size = offset;
    }
}