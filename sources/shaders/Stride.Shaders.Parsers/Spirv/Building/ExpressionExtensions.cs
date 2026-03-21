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

    public static HashSet<Op> KernelSpecConstantOpSupportedOps = new()
    {
        // Note: those are not supported in shaders
        // but we'll make sure to simplify them once they can be resolved.
        // They are needed for more complex constants.
        Op.OpConvertFToS,
        Op.OpConvertFToU,
        Op.OpConvertSToF,
        Op.OpConvertUToF,
        Op.OpUConvert,
        Op.OpConvertPtrToU,
        Op.OpConvertUToPtr,
        Op.OpGenericCastToPtr,
        Op.OpPtrCastToGeneric,
        Op.OpBitcast,
        Op.OpFNegate,
        Op.OpFAdd,
        Op.OpFSub,
        Op.OpFMul,
        Op.OpFDiv,
        Op.OpFRem,
        Op.OpFMod,
        Op.OpAccessChain,
        Op.OpInBoundsAccessChain,
        Op.OpPtrAccessChain,
        Op.OpInBoundsPtrAccessChain,
    };

    public static SpirvValue CompileConstantValue(this Expression expression, SymbolTable table, SpirvContext context, SymbolType? expectedType = null)
    {
        var compiler = new CompilerUnit(context, new());
        expression.ProcessSymbol(table, expectedType);
        var result = expression.CompileAsValue(table, compiler, expectedType);

        if (expectedType != null)
            compiler.Builder.Convert(context, result, expectedType);

        var buffer = compiler.Builder.GetBuffer();

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
            // Rewrite using OpSpecConstantOp when possible
            else if (ShaderSpecConstantOpSupportedOps.Contains(i.Op) || KernelSpecConstantOpSupportedOps.Contains(i.Op))
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
                throw new InvalidOperationException($"OpCode {i.Op} not supported when compiling constant {expression}");
            }
        }

        buffer.Dispose();

        return result;
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
    /// Try to fold a binary/unary operation at compile time when all operands are known OpConstant values.
    /// Returns true and the folded OpConstant instruction if successful.
    /// </summary>
    private static bool TryFoldConstantOp(SpirvContext context, OpDataIndex instruction, out OpData foldedInstruction)
    {
        foldedInstruction = default;
        var span = instruction.Data.Memory.Span;
        var resultType = span[1];
        var resultId = span[2];
        var op = instruction.Op;

        // Get the type info for the result
        if (!context.GetBuffer().TryGetInstructionById(resultType, out var typeInst))
            return false;

        // Handle unary operations (operand at index 3)
        if (span.Length == 4)
        {
            if (!context.TryGetConstantValue(span[3], out var operandVal, out _) || operandVal is null)
                return false;

            object? result = op switch
            {
                Op.OpSNegate when operandVal is int v => (object)(-(int)v),
                Op.OpFNegate when operandVal is float v => -v,
                Op.OpConvertFToS when operandVal is float v => (int)v,
                Op.OpConvertFToU when operandVal is float v => (uint)v,
                Op.OpConvertSToF when operandVal is int v => (float)v,
                Op.OpConvertUToF when operandVal is uint v => (float)v,
                _ => null,
            };
            if (result is null) return false;
            foldedInstruction = EmitFoldedConstant(resultType, resultId, result, typeInst);
            return true;
        }

        // Handle binary operations (operands at index 3, 4)
        if (span.Length == 5)
        {
            if (!context.TryGetConstantValue(span[3], out var leftVal, out _) || leftVal is null)
                return false;
            if (!context.TryGetConstantValue(span[4], out var rightVal, out _) || rightVal is null)
                return false;

            object? result = op switch
            {
                Op.OpIAdd when leftVal is int l && rightVal is int r => (object)(l + r),
                Op.OpISub when leftVal is int l && rightVal is int r => l - r,
                Op.OpIMul when leftVal is int l && rightVal is int r => l * r,
                Op.OpSDiv when leftVal is int l && rightVal is int r => l / r,
                Op.OpFAdd when leftVal is float l && rightVal is float r => l + r,
                Op.OpFSub when leftVal is float l && rightVal is float r => l - r,
                Op.OpFMul when leftVal is float l && rightVal is float r => l * r,
                Op.OpFDiv when leftVal is float l && rightVal is float r => l / r,
                Op.OpIAdd when leftVal is uint l && rightVal is uint r => l + r,
                Op.OpISub when leftVal is uint l && rightVal is uint r => l - r,
                Op.OpIMul when leftVal is uint l && rightVal is uint r => l * r,
                Op.OpUDiv when leftVal is uint l && rightVal is uint r => l / r,
                _ => null,
            };
            if (result is null) return false;
            foldedInstruction = EmitFoldedConstant(resultType, resultId, result, typeInst);
            return true;
        }

        return false;
    }

    private static OpData EmitFoldedConstant(int resultType, int resultId, object value, OpDataIndex typeInst)
    {
        return value switch
        {
            int v => new OpData(new OpConstant<int>(resultType, resultId, v).InstructionMemory),
            uint v => new OpData(new OpConstant<uint>(resultType, resultId, v).InstructionMemory),
            float v => new OpData(new OpConstant<float>(resultType, resultId, v).InstructionMemory),
            double v => new OpData(new OpConstant<double>(resultType, resultId, v).InstructionMemory),
            long v => new OpData(new OpConstant<long>(resultType, resultId, v).InstructionMemory),
            ulong v => new OpData(new OpConstant<ulong>(resultType, resultId, v).InstructionMemory),
            _ => throw new NotSupportedException($"Cannot fold constant of type {value.GetType()}"),
        };
    }
}
