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
        var result = expression.TryCompileConstantInteger(table, context, out _, out var evaluated) && evaluated != null;
        value = evaluated ?? 0;
        return result;
    }

    /// <summary>
    /// Compiles an int or uint constant expression. Its value is null when it is not known yet, which is when it depends
    /// on a generic that is not resolved.
    /// </summary>
    public static bool TryCompileConstantInteger(this Expression expression, SymbolTable table, SpirvContext context, out SpirvValue constant, out int? value)
    {
        value = null;
        if (!expression.TryCompileConstantValue(table, context, out constant)
            || context.ReverseTypes[constant.TypeId] is not ScalarType { Type: Scalar.Int or Scalar.UInt })
            return false;

        if (context.TryGetConstantValue(constant.Id, out var evaluated, out _))
            value = evaluated is uint u ? unchecked((int)u) : (int)evaluated;
        return true;
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

            // Every instruction of the expression becomes a constant of the context
            for (int index = 0; index < buffer.Count; ++index)
            {
                var i = buffer[index];
                if (GetConstantIdOperandCount(i) < 0)
                    throw new NotConstantExpressionException($"'{expression}' is not a compile-time constant ({i.Op} can't be part of one)");

                result = AddAsConstant(context, i);
            }

            return result;
        }
        finally
        {
            buffer.Dispose();
        }
    }

    /// <summary>
    /// Turns a value computed in a function into a constant of the context, when the instructions computing it can all be
    /// part of a constant (e.g. the `int2(Step * 2, 1)` of a texture offset). They move out of the function and keep their ids.
    /// </summary>
    public static bool TryHoistAsConstant(SpirvContext context, SpirvBuffer buffer, int id)
    {
        if (IsConstant(context, id))
            return true;

        if (!buffer.TryGetInstructionById(id, out var i))
            return false;
        var idOperandCount = GetConstantIdOperandCount(i);
        if (idOperandCount < 0)
            return false;

        // Operands first: a constant is defined before it is used
        for (var index = 0; index < idOperandCount; index++)
        {
            if (!TryHoistAsConstant(context, buffer, i.Data.Memory.Span[3 + index]))
                return false;
        }

        AddAsConstant(context, i);
        SpirvBuilder.SetOpNop(i.Data.Memory.Span);
        return true;
    }

    /// <summary>
    /// Returns true if the given ID refers to a constant of the context (which might not be resolved yet).
    /// </summary>
    public static bool IsConstant(SpirvContext context, int id)
    {
        return context.GetBuffer().TryGetInstructionById(id, out var inst)
            && inst.Op is Op.OpConstant or Op.OpConstantTrue or Op.OpConstantFalse or Op.OpConstantComposite or Op.OpConstantNull
                or Op.OpSpecConstant or Op.OpSpecConstantTrue or Op.OpSpecConstantFalse or Op.OpSpecConstantComposite or Op.OpSpecConstantOp
                or Op.OpGenericParameterSDSL or Op.OpGenericReferenceSDSL;
    }

    /// <summary>
    /// Number of leading operands of an instruction that are ids (the others are literals), or -1 if it can't be part of a constant.
    /// </summary>
    /// <remarks>
    /// An operation that SPIR-V does not allow in a constant of a shader module (e.g. OpBitcast, OpFMul) is needed for more complex
    /// constants: the mixer simplifies it once it can be resolved, so it has to be one that ConstantEvaluator knows.
    /// </remarks>
    private static int GetConstantIdOperandCount(OpDataIndex i)
    {
        return i.Op switch
        {
            Op.OpCompositeConstruct => i.Data.Memory.Length - 3,
            _ when ConstantEvaluator.GetIdOperandCount(i.Op) is > 0 and var count => count,
            _ => -1,
        };
    }

    // Adds the constant form of an instruction to the context, with the same result id
    private static SpirvValue AddAsConstant(SpirvContext context, OpDataIndex i)
    {
        var span = i.Data.Memory.Span;
        var resultType = span[1];
        var resultId = span[2];

        if (i.Op == Op.OpCompositeConstruct)
        {
            // OpConstantComposite if all constituents are plain constants, OpSpecConstantComposite otherwise
            var allConstant = true;
            for (int j = 3; j < span.Length; j++)
                allConstant &= IsPlainConstant(context, span[j]);

            Span<int> instruction = [.. span];
            instruction[0] = (int)(allConstant ? Op.OpConstantComposite : Op.OpSpecConstantComposite) | (instruction.Length << 16);
            context.Add(new OpData(instruction));
        }
        // When all operands are known constant values, fold at compile time to avoid
        // OpSpecConstantOp which some SPIR-V backends (e.g. SPIRV-Cross) don't fully support.
        else if (TryFoldConstantOp(context, i, out var foldedInstruction))
        {
            context.Add(foldedInstruction);
        }
        else
        {
            Span<int> instruction = [(int)Op.OpSpecConstantOp, resultType, resultId, (int)i.Op, .. span[3..]];
            instruction[0] |= instruction.Length << 16;
            context.Add(new OpData(instruction));
        }

        return new(resultId, resultType);
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
