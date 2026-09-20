using System.Collections.Frozen;
using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public static class ExpressionExtensions
{
    /// <summary>
    /// Operations that OpSpecConstantOp accepts in a shader module (SPIR-V accepts more with the Kernel capability).
    /// </summary>
    public static readonly FrozenSet<Op> ShaderSpecConstantOpSupportedOps = new HashSet<Op>
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
    }.ToFrozenSet();

    /// <summary>
    /// Same as <see cref="CompileConstantValue"/>, for an expression that the shader code does not guarantee to be a constant.
    /// </summary>
    public static bool TryCompileConstantValue(this Expression expression, SymbolTable table, SpirvContext context, out SpirvValue result, SymbolType? expectedType = null)
    {
        try
        {
            result = expression.CompileConstantValue(table, context, expectedType);
            return true;
        }
        catch (NotConstantExpressionException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Evaluates an int or uint expression whose value must be known when compiling, because it ends up as a literal
    /// (e.g. a switch case label or a [numthreads] parameter). It can't depend on a generic that is not resolved yet.
    /// </summary>
    public static bool TryEvaluateConstantInteger(this Expression expression, SymbolTable table, SpirvContext context, out int value)
    {
        value = 0;
        if (!expression.TryCompileConstantValue(table, context, out var constant)
            || !ConstantExpression.ParseFromBuffer(constant.Id, context.GetBuffer(), context).TryEvaluate(out var evaluated))
            return false;

        switch (evaluated)
        {
            case int i:
                value = i;
                return true;
            case uint u:
                value = unchecked((int)u);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Compiles an expression as a constant of the context.
    /// </summary>
    /// <exception cref="NotConstantExpressionException">The expression is not a compile-time constant.</exception>
    public static SpirvValue CompileConstantValue(this Expression expression, SymbolTable table, SpirvContext context, SymbolType? expectedType = null)
    {
        var compiler = new CompilerUnit(context, new());
        var buffer = compiler.Builder.GetBuffer();
        try
        {
            expression.ProcessSymbol(table, expectedType);
            var result = expression.CompileAsValue(table, compiler, expectedType);

            if (expectedType != null)
                result = compiler.Builder.Convert(context, result, expectedType);

            // Process each instruction and check if it can be converted to constant version.
            // When all operands are known OpConstant values, fold at compile time to avoid
            // OpSpecConstantOp which some SPIR-V backends (e.g. SPIRV-Cross) don't fully support.
            for (int index = 0; index < buffer.Count; ++index)
            {
                var i = buffer[index];

                if (i.Op == Op.OpCompositeConstruct)
                {
                    // Check if all constituents are plain OpConstant — if so, use OpConstantComposite instead of OpSpecConstantComposite.
                    var span = i.Data.Memory.Span;
                    bool allConstant = true;
                    for (int j = 3; j < span.Length; j++)
                    {
                        if (!IsPlainConstant(context, span[j]))
                        {
                            allConstant = false;
                            break;
                        }
                    }

                    if (allConstant)
                    {
                        i.Data.Memory.Span[0] = (int)Op.OpConstantComposite | (i.Data.Memory.Length << 16);
                    }
                    else
                    {
                        i.Data.Memory.Span[0] = (int)Op.OpSpecConstantComposite | (i.Data.Memory.Length << 16);
                    }

                    var instruction = context.Add(new(i.Data.Memory.Span));
                    result = new(instruction.Data);
                }
                // Rewrite using OpSpecConstantOp when possible.
                // An operation that is not allowed in a shader module (e.g. OpBitcast, OpFMul) is needed for more complex constants:
                // the mixer simplifies it once it can be resolved, so it has to be one that ConstantEvaluator knows.
                else if (ShaderSpecConstantOpSupportedOps.Contains(i.Op) || ConstantEvaluator.GetOperandCount(i.Op) != 0)
                {
                    var resultType = i.Data.Memory.Span[1];
                    var resultId = i.Data.Memory.Span[2];

                    // Try to fold: if all operands are known constants, compute the result at compile time.
                    if (TryFoldConstantOp(context, i, out var foldedInstruction))
                    {
                        context.Add(foldedInstruction);
                        result = new(resultId, resultType);
                    }
                    else
                    {
                        Span<int> instruction = [(int)Op.OpSpecConstantOp, resultType, resultId, (int)i.Op, .. i.Data.Memory.Span[3..]];
                        instruction[0] |= instruction.Length << 16;
                        context.Add(new OpData(instruction));
                        result = new(resultId, resultType);
                    }
                }
                else
                {
                    throw new NotConstantExpressionException($"'{expression}' is not a compile-time constant ({i.Op} can't be part of one)");
                }
            }

            return result;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    /// <summary>
    /// Returns true if the given ID refers to an OpConstant, OpConstantTrue, OpConstantFalse,
    /// or OpConstantComposite (i.e. not a spec constant).
    /// </summary>
    private static bool IsPlainConstant(SpirvContext context, int id)
    {
        if (!context.GetBuffer().TryGetInstructionById(id, out var inst))
            return false;
        return inst.Op is Op.OpConstant or Op.OpConstantTrue or Op.OpConstantFalse or Op.OpConstantComposite or Op.OpConstantNull;
    }

    /// <summary>
    /// Try to fold a unary/binary operation at compile time when all operands are known constant values.
    /// Returns true and the folded constant instruction if successful.
    /// </summary>
    private static bool TryFoldConstantOp(SpirvContext context, OpDataIndex instruction, out OpData foldedInstruction)
    {
        foldedInstruction = default;
        var span = instruction.Data.Memory.Span;
        var resultType = span[1];
        var resultId = span[2];

        if (!context.ReverseTypes.TryGetValue(resultType, out var resultSymbolType))
            return false;

        // Note: the operation decides how many operands are ids, the length of the instruction does not
        object? result;
        switch (ConstantEvaluator.GetEvaluatedOperandCount(instruction.Op))
        {
            // Unary operation (operand at index 3)
            case 1:
                if (!context.TryGetConstantValue(span[3], out var operandVal, out _)
                    || !ConstantEvaluator.TryEvaluateUnary(instruction.Op, operandVal, resultSymbolType, out result))
                    return false;
                break;
            // Binary operation (operands at index 3, 4)
            case 2:
                if (!context.TryGetConstantValue(span[3], out var leftVal, out _)
                    || !context.TryGetConstantValue(span[4], out var rightVal, out _)
                    || !ConstantEvaluator.TryEvaluateBinary(instruction.Op, leftVal, rightVal, out result))
                    return false;
                break;
            default:
                return false;
        }

        foldedInstruction = SpirvContext.CreateConstantInstruction(resultType, resultId, result);
        return true;
    }
}
