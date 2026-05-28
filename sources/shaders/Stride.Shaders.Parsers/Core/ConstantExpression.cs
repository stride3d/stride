using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Core;

/// <summary>
/// Context-independent constant expression with value equality.
/// Replaces raw SPIR-V ID + buffer references for array sizes and generic arguments.
/// </summary>
public abstract record ConstantExpression
{
    /// <summary>
    /// Emit into a SPIR-V context, returning the result ID. Handles dedup via existing context caches.
    /// </summary>
    public abstract int Emit(SpirvContext context);

    /// <summary>
    /// Try to evaluate to a concrete value without SPIR-V context.
    /// Returns false for unresolved generic parameters and expressions containing them.
    /// </summary>
    public abstract bool TryEvaluate(out object? value);

    /// <summary>
    /// Replace GenericParamExpr nodes matching the given declaringClass with resolved values.
    /// Returns this if no substitution occurred.
    /// </summary>
    public virtual ConstantExpression Substitute(string declaringClass, ConstantExpression[] args) => this;

    /// <summary>
    /// Look up the ResultType of an emitted instruction by its ID.
    /// </summary>
    protected static int GetEmittedTypeId(SpirvContext context, int instructionId)
    {
        if (context.GetBuffer().TryGetInstructionById(instructionId, out var inst))
        {
            if (inst.Data.IdResultType is int typeId)
                return typeId;
        }
        throw new InvalidOperationException($"Cannot determine type for emitted instruction {instructionId}");
    }

    /// <summary>
    /// Emit the expression into a temporary standalone SpirvBuffer.
    /// Used when the expression needs to be imported into another context via InsertWithoutDuplicates.
    /// </summary>
    public SpirvBuffer EmitToBuffer()
    {
        var tempContext = new SpirvContext();
        var resultId = Emit(tempContext);
        return SpirvContext.ExtractConstantFromBuffer(resultId, tempContext.GetBuffer());
    }

    /// <summary>
    /// Create a ConstantExpression from a concrete runtime value.
    /// </summary>
    public static ConstantExpression FromValue(object value) => value switch
    {
        int i => new IntConstExpr(i),
        uint u => new IntConstExpr(u),
        long l => new IntConstExpr(l),
        ulong u => new IntConstExpr((long)u),
        float f => new FloatConstExpr(f),
        double d => new FloatConstExpr(d),
        bool b => new BoolConstExpr(b),
        string s => new StringConstExpr(s),
        _ => throw new NotSupportedException($"Unsupported constant type: {value.GetType()}")
    };

    /// <summary>
    /// Parse a SPIR-V constant ID into a ConstantExpression tree.
    /// Replaces ExtractConstantFromBuffer for array sizes and generic arguments.
    /// </summary>
    public static ConstantExpression ParseFromBuffer(int constantId, SpirvBuffer buffer, SpirvContext context)
    {
        if (!buffer.TryGetInstructionById(constantId, out var inst))
            throw new InvalidOperationException($"Cannot find instruction for id {constantId}");

        return ParseInstruction(inst, buffer, context);
    }

    private static ConstantExpression ParseInstruction(OpDataIndex inst, SpirvBuffer buffer, SpirvContext context)
    {
        switch (inst.Op)
        {
            case Op.OpConstantTrue:
                return new BoolConstExpr(true);

            case Op.OpConstantFalse:
                return new BoolConstExpr(false);

            case Op.OpConstant:
            case Op.OpSpecConstant:
            {
                var typeId = inst.Data.Memory.Span[1];
                var operand = inst.Data.Get("value");
                if (buffer.TryGetInstructionById(typeId, out var typeInst))
                {
                    if (typeInst.Op == Op.OpTypeInt)
                    {
                        var type = (OpTypeInt)typeInst;
                        long val = type switch
                        {
                            { Width: <= 32, Signedness: 0 } => (long)operand.ToLiteral<uint>(),
                            { Width: <= 32, Signedness: 1 } => operand.ToLiteral<int>(),
                            { Width: 64, Signedness: 0 } => (long)operand.ToLiteral<ulong>(),
                            { Width: 64, Signedness: 1 } => operand.ToLiteral<long>(),
                            _ => throw new NotImplementedException($"Unsupported int width {type.Width}"),
                        };
                        return new IntConstExpr(val);
                    }
                    else if (typeInst.Op == Op.OpTypeFloat)
                    {
                        var type = new OpTypeFloat(typeInst);
                        double val = type switch
                        {
                            { Width: 16 } => (double)operand.ToLiteral<Half>(),
                            { Width: 32 } => operand.ToLiteral<float>(),
                            { Width: 64 } => operand.ToLiteral<double>(),
                            _ => throw new NotImplementedException($"Unsupported float width {type.Width}"),
                        };
                        return new FloatConstExpr(val);
                    }
                    else
                        throw new NotImplementedException($"Unsupported constant type {typeInst.Op}");
                }
                throw new InvalidOperationException($"Cannot find type instruction for id {typeId}");
            }

            case Op.OpConstantStringSDSL:
            {
                var operand = inst.Data.Get("literalString");
                return new StringConstExpr(operand.ToLiteral<string>());
            }

            case Op.OpGenericParameterSDSL:
            case Op.OpGenericReferenceSDSL:
            {
                var genParam = (OpGenericParameterSDSL)inst;
                return new GenericParamExpr(genParam.Index, genParam.DeclaringClass);
            }

            case Op.OpConstantComposite:
            case Op.OpSpecConstantComposite:
            {
                var typeId = inst.Data.Memory.Span[1];
                if (!context.ReverseTypes.TryGetValue(typeId, out var compositeType))
                    throw new InvalidOperationException($"Cannot find type for composite constant type id {typeId}");
                var constituents = inst.Data.Memory.Span[3..];
                var components = new ConstantExpression[constituents.Length];
                for (int i = 0; i < constituents.Length; i++)
                    components[i] = ParseFromBuffer(constituents[i], buffer, context);
                return new CompositeConstExpr(compositeType, components);
            }

            case Op.OpSpecConstantOp:
            {
                var op = (Op)inst.Data.Memory.Span[3];
                switch (op)
                {
                    // Conversions (unary)
                    case Op.OpConvertFToS:
                    case Op.OpConvertFToU:
                    case Op.OpConvertSToF:
                    case Op.OpConvertUToF:
                    case Op.OpSNegate:
                    case Op.OpFNegate:
                    case Op.OpNot:
                    case Op.OpLogicalNot:
                    {
                        var operandExpr = ParseFromBuffer(inst.Data.Memory.Span[4], buffer, context);
                        return new UnaryOpExpr(op, operandExpr);
                    }
                    // Binary operations
                    case Op.OpIAdd:
                    case Op.OpISub:
                    case Op.OpIMul:
                    case Op.OpUDiv:
                    case Op.OpSDiv:
                    case Op.OpFAdd:
                    case Op.OpFSub:
                    case Op.OpFMul:
                    case Op.OpFDiv:
                    case Op.OpShiftRightLogical:
                    case Op.OpShiftRightArithmetic:
                    case Op.OpShiftLeftLogical:
                    case Op.OpBitwiseOr:
                    case Op.OpBitwiseXor:
                    case Op.OpBitwiseAnd:
                    case Op.OpLogicalOr:
                    case Op.OpLogicalAnd:
                    case Op.OpLogicalEqual:
                    case Op.OpLogicalNotEqual:
                    case Op.OpIEqual:
                    case Op.OpINotEqual:
                    case Op.OpULessThan:
                    case Op.OpSLessThan:
                    case Op.OpUGreaterThan:
                    case Op.OpSGreaterThan:
                    case Op.OpULessThanEqual:
                    case Op.OpSLessThanEqual:
                    case Op.OpUGreaterThanEqual:
                    case Op.OpSGreaterThanEqual:
                    {
                        var left = ParseFromBuffer(inst.Data.Memory.Span[4], buffer, context);
                        var right = ParseFromBuffer(inst.Data.Memory.Span[5], buffer, context);
                        return new BinaryOpExpr(op, left, right);
                    }
                    case Op.OpSelect:
                    {
                        var cond = ParseFromBuffer(inst.Data.Memory.Span[4], buffer, context);
                        var trueVal = ParseFromBuffer(inst.Data.Memory.Span[5], buffer, context);
                        var falseVal = ParseFromBuffer(inst.Data.Memory.Span[6], buffer, context);
                        return new SelectExpr(cond, trueVal, falseVal);
                    }
                    default:
                        throw new NotImplementedException($"Unsupported OpSpecConstantOp inner op: {op}");
                }
            }

            default:
                throw new NotImplementedException($"Cannot parse constant expression from {inst.Op}");
        }
    }
}

/// <summary>
/// Integer constant. Covers int, uint, long, ulong — signedness determined at emission by SPIR-V type context.
/// </summary>
public sealed record IntConstExpr(long Value) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // For values that fit in int, use int (signed) — matches the common case for array sizes
        if (Value is >= int.MinValue and <= int.MaxValue)
            return context.CompileConstant((int)Value).Id;
        return context.CompileConstant(Value).Id;
    }

    public override bool TryEvaluate(out object? value)
    {
        if (Value is >= int.MinValue and <= int.MaxValue)
            value = (int)Value;
        else
            value = Value;
        return true;
    }

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Float constant. Covers float, double — precision determined at emission.
/// </summary>
public sealed record FloatConstExpr(double Value) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Use float precision if the value fits without loss
        var asFloat = (float)Value;
        if ((double)asFloat == Value)
            return context.CompileConstant(asFloat).Id;
        return context.CompileConstant(Value).Id;
    }

    public override bool TryEvaluate(out object? value)
    {
        var asFloat = (float)Value;
        if ((double)asFloat == Value)
            value = asFloat;
        else
            value = Value;
        return true;
    }

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Boolean constant.
/// </summary>
public sealed record BoolConstExpr(bool Value) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        return context.CompileConstant(Value).Id;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = Value;
        return true;
    }

    public override string ToString() => Value.ToString();
}

/// <summary>
/// String constant. Used for LinkType, Semantic, and MemberName generic parameters.
/// Emits as OpConstantStringSDSL.
/// </summary>
public sealed record StringConstExpr(string Value) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        var id = context.Bound++;
        context.Add(new OpConstantStringSDSL(id, Value));
        return id;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = Value;
        return true;
    }

    public override string ToString() => $"\"{Value}\"";
}

/// <summary>
/// Reference to a generic parameter from a (possibly ancestor) shader.
/// Replaces both OpGenericParameterSDSL and OpGenericReferenceSDSL —
/// the distinction disappears at the expression level.
/// </summary>
public sealed record GenericParamExpr(int Index, string DeclaringClass) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Emit as GenericReference — when this appears in a child context,
        // it references the parent's generic parameter
        var typeId = context.GetOrRegister(ScalarType.Int);
        var id = context.Bound++;
        context.Add(new OpGenericReferenceSDSL(typeId, id, Index, DeclaringClass));
        return id;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        return false;
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
        => declaringClass == DeclaringClass && Index < args.Length ? args[Index] : this;

    public override string ToString() => $"{DeclaringClass}[{Index}]";
}

/// <summary>
/// Unary spec-constant operation (SNegate, FNegate, Not, LogicalNot, conversions).
/// </summary>
public sealed record UnaryOpExpr(Op Op, ConstantExpression Operand) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Try constant folding first — avoids OpSpecConstantOp which some backends don't support.
        if (TryEvaluate(out var folded) && folded is not null)
            return FromValue(folded).Emit(context);

        var operandId = Operand.Emit(context);
        var operandTypeId = GetEmittedTypeId(context, operandId);
        var resultTypeId = ResolveResultType(context, operandTypeId);
        var resultId = context.Bound++;
        Span<int> instruction = [(int)Specification.Op.OpSpecConstantOp, resultTypeId, resultId, (int)Op, operandId];
        instruction[0] |= instruction.Length << 16;
        context.Add(new OpData(instruction));
        return resultId;
    }

    private int ResolveResultType(SpirvContext context, int operandTypeId)
    {
        return Op switch
        {
            // Conversions change the result type
            Specification.Op.OpConvertFToS => context.GetOrRegister(ScalarType.Int),
            Specification.Op.OpConvertFToU => context.GetOrRegister(ScalarType.UInt),
            Specification.Op.OpConvertSToF => context.GetOrRegister(ScalarType.Float),
            Specification.Op.OpConvertUToF => context.GetOrRegister(ScalarType.Float),
            // Unary ops keep the same type
            _ => operandTypeId,
        };
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        if (!Operand.TryEvaluate(out var operandValue) || operandValue is null)
            return false;

        value = Op switch
        {
            Specification.Op.OpSNegate => (object)(-(int)operandValue),
            Specification.Op.OpFNegate => -(float)operandValue,
            Specification.Op.OpNot when operandValue is int i => ~i,
            Specification.Op.OpLogicalNot when operandValue is bool b => !b,
            Specification.Op.OpConvertFToS => (int)(float)operandValue,
            Specification.Op.OpConvertFToU => (uint)(float)operandValue,
            Specification.Op.OpConvertSToF => (float)(int)operandValue,
            Specification.Op.OpConvertUToF => (float)(uint)operandValue,
            _ => null,
        };
        return value is not null;
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newOperand = Operand.Substitute(declaringClass, args);
        if (ReferenceEquals(newOperand, Operand)) return this;
        var result = new UnaryOpExpr(Op, newOperand);
        return result.TryEvaluate(out var val) && val is not null ? FromValue(val) : result;
    }

    public override string ToString() => $"{Op}({Operand})";
}

/// <summary>
/// Binary spec-constant operation (IAdd, IMul, ISub, etc.)
/// </summary>
public sealed record BinaryOpExpr(Op Op, ConstantExpression Left, ConstantExpression Right) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Try constant folding first — avoids OpSpecConstantOp which some backends don't support.
        if (TryEvaluate(out var folded) && folded is not null)
            return FromValue(folded).Emit(context);

        var leftId = Left.Emit(context);
        var rightId = Right.Emit(context);
        var resultTypeId = GetEmittedTypeId(context, leftId);
        var resultId = context.Bound++;
        Span<int> instruction = [(int)Specification.Op.OpSpecConstantOp, resultTypeId, resultId, (int)Op, leftId, rightId];
        instruction[0] |= instruction.Length << 16;
        context.Add(new OpData(instruction));
        return resultId;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        if (!Left.TryEvaluate(out var leftVal) || leftVal is null)
            return false;
        if (!Right.TryEvaluate(out var rightVal) || rightVal is null)
            return false;

        value = Op switch
        {
            Specification.Op.OpIAdd when leftVal is int l && rightVal is int r => (object)(l + r),
            Specification.Op.OpISub when leftVal is int l && rightVal is int r => l - r,
            Specification.Op.OpIMul when leftVal is int l && rightVal is int r => l * r,
            Specification.Op.OpSDiv when leftVal is int l && rightVal is int r => l / r,
            Specification.Op.OpUDiv when leftVal is uint l && rightVal is uint r => l / r,
            Specification.Op.OpFAdd when leftVal is float l && rightVal is float r => l + r,
            Specification.Op.OpFSub when leftVal is float l && rightVal is float r => l - r,
            Specification.Op.OpFMul when leftVal is float l && rightVal is float r => l * r,
            Specification.Op.OpFDiv when leftVal is float l && rightVal is float r => l / r,
            Specification.Op.OpBitwiseAnd when leftVal is int l && rightVal is int r => l & r,
            Specification.Op.OpBitwiseOr when leftVal is int l && rightVal is int r => l | r,
            Specification.Op.OpBitwiseXor when leftVal is int l && rightVal is int r => l ^ r,
            Specification.Op.OpShiftLeftLogical when leftVal is int l && rightVal is int r => l << r,
            Specification.Op.OpShiftRightArithmetic when leftVal is int l && rightVal is int r => l >> r,
            Specification.Op.OpLogicalAnd when leftVal is bool l && rightVal is bool r => l && r,
            Specification.Op.OpLogicalOr when leftVal is bool l && rightVal is bool r => l || r,
            Specification.Op.OpLogicalEqual when leftVal is bool l && rightVal is bool r => l == r,
            Specification.Op.OpLogicalNotEqual when leftVal is bool l && rightVal is bool r => l != r,
            Specification.Op.OpIEqual when leftVal is int l && rightVal is int r => l == r,
            Specification.Op.OpINotEqual when leftVal is int l && rightVal is int r => l != r,
            Specification.Op.OpSLessThan when leftVal is int l && rightVal is int r => l < r,
            Specification.Op.OpSGreaterThan when leftVal is int l && rightVal is int r => l > r,
            Specification.Op.OpSLessThanEqual when leftVal is int l && rightVal is int r => l <= r,
            Specification.Op.OpSGreaterThanEqual when leftVal is int l && rightVal is int r => l >= r,
            _ => null,
        };
        return value is not null;
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newLeft = Left.Substitute(declaringClass, args);
        var newRight = Right.Substitute(declaringClass, args);
        if (ReferenceEquals(newLeft, Left) && ReferenceEquals(newRight, Right)) return this;
        var result = new BinaryOpExpr(Op, newLeft, newRight);
        return result.TryEvaluate(out var val) && val is not null ? FromValue(val) : result;
    }

    public override string ToString() => $"({Left} {Op} {Right})";
}

/// <summary>
/// Ternary select (OpSelect).
/// </summary>
public sealed record SelectExpr(ConstantExpression Cond, ConstantExpression TrueVal, ConstantExpression FalseVal) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Try constant folding first — avoids OpSpecConstantOp which some backends don't support.
        if (TryEvaluate(out var folded) && folded is not null)
            return FromValue(folded).Emit(context);

        var condId = Cond.Emit(context);
        var trueId = TrueVal.Emit(context);
        var falseId = FalseVal.Emit(context);
        var resultTypeId = GetEmittedTypeId(context, trueId);
        var resultId = context.Bound++;
        Span<int> instruction = [(int)Specification.Op.OpSpecConstantOp, resultTypeId, resultId, (int)Op.OpSelect, condId, trueId, falseId];
        instruction[0] |= instruction.Length << 16;
        context.Add(new OpData(instruction));
        return resultId;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        if (!Cond.TryEvaluate(out var condVal) || condVal is not bool b)
            return false;
        return b ? TrueVal.TryEvaluate(out value) : FalseVal.TryEvaluate(out value);
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newCond = Cond.Substitute(declaringClass, args);
        var newTrue = TrueVal.Substitute(declaringClass, args);
        var newFalse = FalseVal.Substitute(declaringClass, args);
        if (ReferenceEquals(newCond, Cond) && ReferenceEquals(newTrue, TrueVal) && ReferenceEquals(newFalse, FalseVal))
            return this;
        var result = new SelectExpr(newCond, newTrue, newFalse);
        return result.TryEvaluate(out var val) && val is not null ? FromValue(val) : result;
    }

    public override string ToString() => $"({Cond} ? {TrueVal} : {FalseVal})";
}

/// <summary>
/// Composite constant (vector, array, struct). Stores constituent expressions and the composite type.
/// </summary>
public sealed record CompositeConstExpr(SymbolType Type, ConstantExpression[] Components) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        Span<int> componentIds = stackalloc int[Components.Length];
        for (int i = 0; i < Components.Length; i++)
            componentIds[i] = Components[i].Emit(context);
        var typeId = context.GetOrRegister(Type);
        var resultId = context.Bound++;
        context.AddData(new OpConstantComposite(typeId, resultId, new(componentIds)));
        return resultId;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        return false;
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newComponents = new ConstantExpression[Components.Length];
        bool changed = false;
        for (int i = 0; i < Components.Length; i++)
        {
            newComponents[i] = Components[i].Substitute(declaringClass, args);
            if (!ReferenceEquals(newComponents[i], Components[i]))
                changed = true;
        }
        return changed ? new CompositeConstExpr(Type, newComponents) : this;
    }

    public bool Equals(CompositeConstExpr? other) =>
        other is not null && Type == other.Type && Components.AsSpan().SequenceEqual(other.Components);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type);
        foreach (var c in Components)
            hash.Add(c);
        return hash.ToHashCode();
    }

    public override string ToString() => $"composite({Type}, [{string.Join(", ", (object[])Components)}])";
}

