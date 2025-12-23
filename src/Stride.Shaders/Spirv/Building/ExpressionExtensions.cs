using CommunityToolkit.HighPerformance;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public static class ExpressionExtensions
{
    public static HashSet<Op> SpecConstantSupportedOps = new()
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

    public static SpirvValue CompileConstantValue(this Expression expression, SymbolTable table, SpirvContext context)
    {
        var compiler = new CompilerUnit(context, new());
        var result = expression.Compile(table, compiler);
        
        var buffer = compiler.Builder.GetBuffer();

        // Process each instruction and check if it can be converted to constant version
        for (int index = 0; index < buffer.Count; ++index)
        {
            var i = buffer[index];

            // Check if opcode is supported
            if (!SpecConstantSupportedOps.Contains(i.Op))
                throw new InvalidOperationException($"OpCode {i.Op} not supported when compiling constant {expression}");

            var resultType = i.Data.Memory.Span[1];
            var resultId = i.Data.Memory.Span[2];

            Span<int> instruction = [(int)Op.OpSpecConstantOp, resultType, resultId, (int)i.Op, .. i.Data.Memory.Span[3..]];
            instruction[0] |= instruction.Length << 16;

            // TODO: Check no IdRef to things outside context is done

            context.GetBuffer().Add(new OpData(instruction));

            result = new(resultId, resultType);
        }

        buffer.Dispose();

        return result;
    }
}
