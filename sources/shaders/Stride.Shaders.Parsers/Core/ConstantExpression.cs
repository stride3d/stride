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
        int i => new IntConstExpr(i, ScalarType.Int),
        uint u => new IntConstExpr(u, ScalarType.UInt),
        long l => new IntConstExpr(l, ScalarType.Int64),
        ulong u => new IntConstExpr((long)u, ScalarType.UInt64),
        Half h => new FloatConstExpr((double)h, ScalarType.Half),
        float f => new FloatConstExpr(f, ScalarType.Float),
        double d => new FloatConstExpr(d, ScalarType.Double),
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
                        return type switch
                        {
                            { Width: <= 32, Signedness: 0 } => new IntConstExpr(operand.ToLiteral<uint>(), ScalarType.UInt),
                            { Width: <= 32, Signedness: 1 } => new IntConstExpr(operand.ToLiteral<int>(), ScalarType.Int),
                            { Width: 64, Signedness: 0 } => new IntConstExpr((long)operand.ToLiteral<ulong>(), ScalarType.UInt64),
                            { Width: 64, Signedness: 1 } => new IntConstExpr(operand.ToLiteral<long>(), ScalarType.Int64),
                            _ => throw new NotImplementedException($"Unsupported int width {type.Width}"),
                        };
                    }
                    else if (typeInst.Op == Op.OpTypeFloat)
                    {
                        var type = new OpTypeFloat(typeInst);
                        return type switch
                        {
                            { Width: 16 } => new FloatConstExpr((double)operand.ToLiteral<Half>(), ScalarType.Half),
                            { Width: 32 } => new FloatConstExpr(operand.ToLiteral<float>(), ScalarType.Float),
                            { Width: 64 } => new FloatConstExpr(operand.ToLiteral<double>(), ScalarType.Double),
                            _ => throw new NotImplementedException($"Unsupported float width {type.Width}"),
                        };
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

            case Op.OpConstantNull:
            {
                var typeId = inst.Data.Memory.Span[1];
                if (!context.ReverseTypes.TryGetValue(typeId, out var nullType))
                    throw new InvalidOperationException($"Cannot find type for null constant type id {typeId}");
                return new NullConstExpr(nullType);
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
                if (!context.ReverseTypes.TryGetValue(inst.Data.Memory.Span[1], out var resultType))
                    throw new InvalidOperationException($"Cannot find result type of constant {inst.Data.Memory.Span[2]}");
                // Operations on composites: their last operands are literal indices
                if (op is Op.OpCompositeExtract or Op.OpCompositeInsert or Op.OpVectorShuffle)
                {
                    var idOperandCount = ConstantEvaluator.GetIdOperandCount(op);
                    var operands = new ConstantExpression[idOperandCount];
                    for (int i = 0; i < idOperandCount; i++)
                        operands[i] = ParseFromBuffer(inst.Data.Memory.Span[4 + i], buffer, context);
                    return CompositeOpExpr.Create(op, resultType, operands, inst.Data.Memory.Span[(4 + idOperandCount)..].ToArray());
                }

                // Note: the operation decides how many operands are ids, the length of the instruction does not
                switch (ConstantEvaluator.GetEvaluatedOperandCount(op))
                {
                    case 1:
                    {
                        var operandExpr = ParseFromBuffer(inst.Data.Memory.Span[4], buffer, context);
                        return new UnaryOpExpr(op, resultType, operandExpr);
                    }
                    case 2:
                    {
                        var left = ParseFromBuffer(inst.Data.Memory.Span[4], buffer, context);
                        var right = ParseFromBuffer(inst.Data.Memory.Span[5], buffer, context);
                        return new BinaryOpExpr(op, resultType, left, right);
                    }
                    case 3:
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
/// Integer constant of type int, uint, long or ulong (whose value is stored as its bits).
/// </summary>
public sealed record IntConstExpr(long Value, ScalarType Type) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        TryEvaluate(out var value);
        return context.CompileConstant(Type, value!).Id;
    }

    public override bool TryEvaluate(out object? value)
    {
        // Note: first cast to object is important, otherwise all the cases are converted to a common type
        value = Type.Type switch
        {
            Scalar.Int => (object)(int)Value,
            Scalar.UInt => (uint)Value,
            Scalar.Int64 => Value,
            Scalar.UInt64 => (ulong)Value,
            _ => throw new NotSupportedException($"Unsupported integer constant type {Type}"),
        };
        return true;
    }

    public override string ToString() => Type.Type == Scalar.UInt64 ? ((ulong)Value).ToString() : Value.ToString();
}

/// <summary>
/// Float constant of type half, float or double.
/// </summary>
public sealed record FloatConstExpr(double Value, ScalarType Type) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        TryEvaluate(out var value);
        return context.CompileConstant(Type, value!).Id;
    }

    public override bool TryEvaluate(out object? value)
    {
        // Note: first cast to object is important, otherwise all the cases are converted to a common type
        value = Type.Type switch
        {
            Scalar.Half => (object)(Half)Value,
            Scalar.Float => (float)Value,
            Scalar.Double => Value,
            _ => throw new NotSupportedException($"Unsupported float constant type {Type}"),
        };
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
public sealed record UnaryOpExpr(Op Op, SymbolType ResultType, ConstantExpression Operand) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Try constant folding first — avoids OpSpecConstantOp which some backends don't support.
        if (TryEvaluate(out var folded) && folded is not null)
            return context.CompileConstant(ResultType, folded).Id;

        var operandId = Operand.Emit(context);
        var resultId = context.Bound++;
        Span<int> instruction = [(int)Specification.Op.OpSpecConstantOp, context.GetOrRegister(ResultType), resultId, (int)Op, operandId];
        instruction[0] |= instruction.Length << 16;
        context.Add(new OpData(instruction));
        return resultId;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        return Operand.TryEvaluate(out var operandValue) && operandValue is not null
            && ConstantEvaluator.TryEvaluateUnary(Op, operandValue, ResultType, out value);
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newOperand = Operand.Substitute(declaringClass, args);
        if (ReferenceEquals(newOperand, Operand)) return this;
        var result = new UnaryOpExpr(Op, ResultType, newOperand);
        return result.TryEvaluate(out var val) && val is not null ? FromValue(val) : result;
    }

    public override string ToString() => $"{Op}({Operand})";
}

/// <summary>
/// Binary spec-constant operation (IAdd, IMul, ISub, etc.)
/// </summary>
public sealed record BinaryOpExpr(Op Op, SymbolType ResultType, ConstantExpression Left, ConstantExpression Right) : ConstantExpression
{
    public override int Emit(SpirvContext context)
    {
        // Try constant folding first — avoids OpSpecConstantOp which some backends don't support.
        if (TryEvaluate(out var folded) && folded is not null)
            return context.CompileConstant(ResultType, folded).Id;

        var leftId = Left.Emit(context);
        var rightId = Right.Emit(context);
        var resultId = context.Bound++;
        Span<int> instruction = [(int)Specification.Op.OpSpecConstantOp, context.GetOrRegister(ResultType), resultId, (int)Op, leftId, rightId];
        instruction[0] |= instruction.Length << 16;
        context.Add(new OpData(instruction));
        return resultId;
    }

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        return Left.TryEvaluate(out var leftValue) && leftValue is not null
            && Right.TryEvaluate(out var rightValue) && rightValue is not null
            && ConstantEvaluator.TryEvaluateBinary(Op, leftValue, rightValue, out value);
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newLeft = Left.Substitute(declaringClass, args);
        var newRight = Right.Substitute(declaringClass, args);
        if (ReferenceEquals(newLeft, Left) && ReferenceEquals(newRight, Right)) return this;
        var result = new BinaryOpExpr(Op, ResultType, newLeft, newRight);
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

/// <summary>
/// Null constant (OpConstantNull), e.g. the `(StructType)0` zero-initialization.
/// </summary>
public sealed record NullConstExpr(SymbolType Type) : ConstantExpression
{
    public override int Emit(SpirvContext context) => context.CreateDefaultConstantComposite(Type).Id;

    public override bool TryEvaluate(out object? value)
    {
        value = null;
        return false;
    }

    public override string ToString() => $"null({Type})";
}

/// <summary>
/// Spec-constant operation on composites, whose last operands are literal indices: OpCompositeExtract (`v.x`),
/// OpVectorShuffle (`v.xy`) and OpCompositeInsert.
/// </summary>
public sealed record CompositeOpExpr : ConstantExpression
{
    public Op Op { get; }
    public SymbolType ResultType { get; }
    public ConstantExpression[] Operands { get; }
    public int[] Literals { get; }

    private CompositeOpExpr(Op op, SymbolType resultType, ConstantExpression[] operands, int[] literals)
    {
        Op = op;
        ResultType = resultType;
        Operands = operands;
        Literals = literals;
    }

    /// <summary>
    /// Creates the operation, or directly the expression it selects when its operands are composites with known components.
    /// </summary>
    public static ConstantExpression Create(Op op, SymbolType resultType, ConstantExpression[] operands, int[] literals)
    {
        switch (op)
        {
            case Specification.Op.OpCompositeExtract:
            {
                var current = operands[0];
                foreach (var index in literals)
                {
                    if (!TryGetComponents(current, out var components) || index >= components.Length)
                        return new CompositeOpExpr(op, resultType, operands, literals);
                    current = components[index];
                }
                return current;
            }
            case Specification.Op.OpVectorShuffle when TryGetComponents(operands[0], out var left) && TryGetComponents(operands[1], out var right):
            {
                var selected = new ConstantExpression[literals.Length];
                for (int i = 0; i < literals.Length; i++)
                {
                    // Note: 0xFFFFFFFF selects an undefined component
                    if ((uint)literals[i] >= left.Length + right.Length)
                        return new CompositeOpExpr(op, resultType, operands, literals);
                    selected[i] = literals[i] < left.Length ? left[literals[i]] : right[literals[i] - left.Length];
                }
                return new CompositeConstExpr(resultType, selected);
            }
            default:
                return new CompositeOpExpr(op, resultType, operands, literals);
        }
    }

    // A vector can be constructed from other vectors, in which case its constituents are not its components
    private static bool TryGetComponents(ConstantExpression expression, out ConstantExpression[] components)
    {
        components = expression is CompositeConstExpr composite ? composite.Components : [];
        return expression is CompositeConstExpr { Type: var type } && (type is not VectorType vectorType || vectorType.Size == components.Length);
    }

    public override int Emit(SpirvContext context)
    {
        Span<int> operandIds = stackalloc int[Operands.Length];
        for (int i = 0; i < Operands.Length; i++)
            operandIds[i] = Operands[i].Emit(context);

        var resultId = context.Bound++;
        Span<int> instruction = [(int)Specification.Op.OpSpecConstantOp, context.GetOrRegister(ResultType), resultId, (int)Op, .. operandIds, .. Literals];
        instruction[0] |= instruction.Length << 16;
        context.Add(new OpData(instruction));
        return resultId;
    }

    public override bool TryEvaluate(out object? value)
    {
        // Note: Create() already returned the selected expression when it could
        value = null;
        return false;
    }

    public override ConstantExpression Substitute(string declaringClass, ConstantExpression[] args)
    {
        var newOperands = new ConstantExpression[Operands.Length];
        bool changed = false;
        for (int i = 0; i < Operands.Length; i++)
        {
            newOperands[i] = Operands[i].Substitute(declaringClass, args);
            if (!ReferenceEquals(newOperands[i], Operands[i]))
                changed = true;
        }
        return changed ? Create(Op, ResultType, newOperands, Literals) : this;
    }

    public bool Equals(CompositeOpExpr? other) =>
        other is not null && Op == other.Op && ResultType == other.ResultType
        && Operands.AsSpan().SequenceEqual(other.Operands) && Literals.AsSpan().SequenceEqual(other.Literals);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Op);
        hash.Add(ResultType);
        foreach (var operand in Operands)
            hash.Add(operand);
        foreach (var literal in Literals)
            hash.Add(literal);
        return hash.ToHashCode();
    }

    public override string ToString() => $"{Op}({string.Join(", ", (object[])Operands)}; {string.Join(", ", Literals)})";
}

