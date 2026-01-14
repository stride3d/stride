using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public static class ExpressionExtensions
{
    public static HashSet<Op> ShaderSpecConstantOpSupportedOps = new()
    {
        Op.OpSConvert,
        Op.OpUConvert,
        Op.OpFConvert,
        Op.OpSNegate,
        Op.OpNot,
        Op.OpIAdd,
        Op.OpISub,
        Op.OpIMul,
        Op.OpUDiv,
        Op.OpSDiv,
        Op.OpUMod,
        Op.OpSRem,
        Op.OpSMod,
        Op.OpShiftRightLogical,
        Op.OpShiftRightArithmetic,
        Op.OpShiftLeftLogical,
        Op.OpBitwiseOr,
        Op.OpBitwiseXor,
        Op.OpBitwiseAnd,
        Op.OpVectorShuffle,
        Op.OpCompositeExtract,
        Op.OpCompositeInsert,
        Op.OpLogicalOr,
        Op.OpLogicalAnd,
        Op.OpLogicalNot,
        Op.OpLogicalEqual,
        Op.OpLogicalNotEqual,
        Op.OpSelect,
        Op.OpIEqual,
        Op.OpINotEqual,
        Op.OpULessThan,
        Op.OpSLessThan,
        Op.OpUGreaterThan,
        Op.OpSGreaterThan,
        Op.OpULessThanEqual,
        Op.OpSLessThanEqual,
        Op.OpUGreaterThanEqual,
        Op.OpSGreaterThanEqual,
    };
    
    public static HashSet<Op> ComputeSpecConstantOpSupportedOps = new()
    {
        // Note: those are not supported in standard shaders (only compute)
        // but we'll make sure to simplify them once they can be resolved.
        // We need them for SpirvBuilder.Convert() support
        // However, it seems so far the expectedType system seems enough to use float4(int, int, int, int) in generics, so they are not implemented for now
        Op.OpConvertFToS,
        Op.OpConvertFToU,
        Op.OpConvertSToF,
        Op.OpConvertUToF,
        Op.OpFNegate,
        Op.OpFAdd,
        Op.OpFSub,
        Op.OpFMul,
        Op.OpFDiv,
        Op.OpFRem,
        Op.OpFMod,
    };

    public static SpirvValue CompileConstantValue(this Expression expression, SymbolTable table, SpirvContext context, SymbolType? expectedType = null)
    {
        var compiler = new CompilerUnit(context, new());
        var result = expression.CompileAsValue(table, compiler, expectedType);

        if (expectedType != null)
            compiler.Builder.Convert(context, result, expectedType);

        var buffer = compiler.Builder.GetBuffer();

        // Process each instruction and check if it can be converted to constant version
        for (int index = 0; index < buffer.Count; ++index)
        {
            var i = buffer[index];

            if (i.Op == Op.OpCompositeConstruct)
            {
                i.Data.Memory.Span[0] = (int)Op.OpSpecConstantComposite | (i.Data.Memory.Length << 16);

                // TODO: Check no IdRef to things outside context
                var instruction = context.Add(new(i.Data.Memory.Span));
                result = new(instruction.Data);
            }
            // Rewrite using OpSpecConstantOp when possible
            else if(ShaderSpecConstantOpSupportedOps.Contains(i.Op) || ComputeSpecConstantOpSupportedOps.Contains(i.Op))
            {
                var resultType = i.Data.Memory.Span[1];
                var resultId = i.Data.Memory.Span[2];

                Span<int> instruction = [(int)Op.OpSpecConstantOp, resultType, resultId, (int)i.Op, .. i.Data.Memory.Span[3..]];
                instruction[0] |= instruction.Length << 16;

                // TODO: Check no IdRef to things outside context
                context.Add(new OpData(instruction));
                result = new(resultId, resultType);
            }
            else
            {
                throw new InvalidOperationException($"OpCode {i.Op} not supported when compiling constant {expression}");
            }
        }

        buffer.Dispose();

        return result;
    }
}
