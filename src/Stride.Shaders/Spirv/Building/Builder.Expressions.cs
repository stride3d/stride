using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Parsing.SDSL.AST;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public SpirvValue AsValue(SpirvContext context, SpirvValue result)
    {
        var type = context.ReverseTypes[result.TypeId];
        if (type is PointerType pointerType)
        {
            type = pointerType.BaseType;
            var inst = Insert(new OpLoad(context.Types[type], context.Bound++, result.Id, null));
            result = new(inst.ResultId, inst.ResultType);
        }
        return result;
    }

    public SpirvValue BinaryOperation(SpirvContext context, int resultType, in SpirvValue left, Operator op, in SpirvValue right, string? name = null)
    {

        var instruction = (op, context.ReverseTypes[left.TypeId], context.ReverseTypes[right.TypeId]) switch
        {
            (Operator.Plus, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpIAdd(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Plus, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpFAdd(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Minus, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpISub(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Minus, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpFSub(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Mul, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpIMul(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Mul, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpFMul(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Div, SymbolType l, SymbolType r)
                when l.IsUnsignedIntegerVector() && r.IsUnsignedIntegerVector() && SymbolExtensions.SameComponentCount(l, r)
                => Buffer.InsertData(Position++, new OpUDiv(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Div, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpSDiv(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Div, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector()
                => Buffer.InsertData(Position++, new OpFDiv(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Mod, SymbolType l, SymbolType r)
                when l.IsUnsignedIntegerVector() && r.IsUnsignedIntegerVector()
                => Buffer.InsertData(Position++, new OpUMod(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Mod, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertData(Position++, new OpSMod(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Mod, SymbolType l, SymbolType r)
                when l.IsFloating() && r.IsNumber()
                => Buffer.InsertData(Position++, new OpFMod(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.RightShift, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertData(Position++, new OpShiftRightLogical(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.LeftShift, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertData(Position++, new OpShiftRightLogical(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.AND, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertData(Position++, new OpBitwiseAnd(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.OR, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertData(Position++, new OpBitwiseOr(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.XOR, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertData(Position++, new OpBitwiseXor(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.LogicalAND, ScalarType { TypeName: "bool" }, ScalarType { TypeName: "bool" })
                => Buffer.InsertData(Position++, new OpLogicalAnd(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.LogicalOR, ScalarType { TypeName: "bool" }, ScalarType { TypeName: "bool" })
                => Buffer.InsertData(Position++, new OpLogicalOr(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Equals, ScalarType { TypeName: "int" }, ScalarType { TypeName: "int" })
                => Buffer.InsertData(Position++, new OpIEqual(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Lower, ScalarType { TypeName: "int" }, ScalarType { TypeName: "int" })
                => Buffer.InsertData(Position++, new OpSLessThan(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.LowerOrEqual, ScalarType { TypeName: "int" }, ScalarType { TypeName: "int" })
                => Buffer.InsertData(Position++, new OpSLessThanEqual(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Greater, ScalarType { TypeName: "int" }, ScalarType { TypeName: "int" })
                => Buffer.InsertData(Position++, new OpSGreaterThan(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.GreaterOrEqual, ScalarType { TypeName: "int" }, ScalarType { TypeName: "int" })
                => Buffer.InsertData(Position++, new OpSGreaterThanEqual(resultType, context.Bound++, left.Id, right.Id)),

            _ => throw new NotImplementedException()
        };

        if (name is not null)
            context.AddName(instruction.IdResult ?? -1, name);
        return new(instruction, name);
    }

    public SpirvValue Convert(SpirvContext context, in SpirvValue value, in SymbolType castType)
    {
        var valueId = value.Id;
        var valueType = context.ReverseTypes[value.TypeId];
        var originalType = valueType;

        // No conversion necessary?
        if (castType == valueType)
            return value;

        if (castType is StructType || valueType is StructType)
            throw new NotImplementedException("Can't cast between structures (cast from {valueType} to {castType})");

        // We don't support cast with object yet, filter for numeral types
        if ((castType is not ScalarType && castType is not VectorType && castType is not MatrixType)
            || (valueType is not ScalarType && valueType is not VectorType && valueType is not MatrixType))
            throw new NotImplementedException($"Cast only work between numeral types (cast from {valueType} to {castType})");

        Span<int> values = stackalloc int[castType is MatrixType m ? m.Rows : 1];

        // Truncating
        switch (valueType, castType)
        {
            case (ScalarType s1, ScalarType s2):
                values[0] = valueId;
                break;
            case (VectorType v1, ScalarType s2):
                {
                    values[0] = Insert(new OpCompositeExtract(context.GetOrRegister(v1.BaseType), context.Bound++, valueId, [0])).ResultId;
                    valueType = v1.BaseType;
                    break;
                }
            case (VectorType v1, VectorType v2) when v1.Size <= v2.Size:
                throw new InvalidOperationException($"Can't cast from {v1} to {v2} (more components)");
            case (VectorType v1, VectorType v2) when v1.Size > v2.Size:
                {
                    Span<int> valuesTemp = stackalloc int[v2.Size];
                    for (int i = 0; i < v2.Size; ++i)
                        Insert(new OpCompositeExtract(context.GetOrRegister(v1.BaseType), valuesTemp[i] = context.Bound++, valueId, [i]));
                    valueType = new VectorType(v1.BaseType, v2.Size);
                    values[0] = Insert(new OpCompositeConstruct(context.GetOrRegister(valueType), context.Bound++, new LiteralArray<int>(valuesTemp))).ResultId;
                    break;
                }
            case (VectorType v1, MatrixType m2) when v1.Size != m2.Rows * m2.Columns:
                throw new InvalidOperationException($"Can't cast from {v1} to {m2}");
            case (VectorType v1, MatrixType m2) when v1.Size == m2.Rows * m2.Columns:
                throw new NotImplementedException($"Cast from {v1} to {m2} is not implemented (even though it should be valid since number of components is same");
            case (MatrixType m1, ScalarType s2):
                values[0] = Insert(new OpCompositeExtract(context.GetOrRegister(m1.BaseType), context.Bound++, valueId, [0, 0])).ResultId;
                break;
            case (MatrixType m1, VectorType v2) when v2.Size != m1.Rows * m1.Columns:
                throw new InvalidOperationException($"Can't cast from {m1} to {v2}");
            case (MatrixType m1, VectorType v2) when v2.Size == m1.Rows * m1.Columns:
                throw new NotImplementedException($"Cast from {m1} to {v2} is not implemented (even though it should be valid since number of components is same");
            case (MatrixType m1, MatrixType m2) when m1.Rows < m2.Rows || m1.Columns < m2.Columns:
                throw new InvalidOperationException($"Can't cast from {m1} to {m2} (larger matrix)");
            case (MatrixType m1, MatrixType m2) when m1.Rows >= m2.Rows && m1.Columns >= m2.Columns:
                {
                    for (int i = 0; i < m2.Rows; ++i)
                    {
                        values[i] = Insert(new OpCompositeExtract(context.GetOrRegister(new VectorType(m1.BaseType, m1.Columns)), context.Bound++, valueId, [i])).ResultId;
                        if (m1.Columns != m2.Columns)
                        {
                            Span<int> shuffleIndices = stackalloc int[m2.Columns];
                            for (int j = 0; j < m2.Columns; ++j)
                                shuffleIndices[j] = j;
                            values[i] = Insert(new OpVectorShuffle(context.GetOrRegister(new VectorType(m1.BaseType, m2.Columns)), context.Bound++, values[i], values[i], new(shuffleIndices))).ResultId;
                        }
                    }
                    valueType = new VectorType(m1.BaseType, m2.Columns);
                    break;
                }
        }

        // Type casting
        // (process each vector one by one)
        (int elementSize, var castTypeSameSize) = valueType switch
        {
            ScalarType s => (1, (SymbolType)castType.GetElementType()),
            VectorType s => (s.Size, new VectorType(castType.GetElementType(), s.Size)),
        };
        for (int i = 0; i < values.Length; ++i)
        {
            var rowValue = values[i];
            if (rowValue == 0)
                throw new InvalidOperationException($"Type conversion from {originalType} to {castType} failed during conversion (current type: {valueType})");

            var typeCasting = (valueType.GetElementType(), castType.GetElementType()) switch
            {
                // https://learn.microsoft.com/en-us/windows/win32/direct3d9/casting-and-conversion
                (ScalarType { TypeName: "float" }, ScalarType { TypeName: "int" }) => InsertData(new OpConvertFToS(context.GetOrRegister(castTypeSameSize), context.Bound++, rowValue)),

                (ScalarType { TypeName: "float" }, ScalarType { TypeName: "bool" }) => InsertData(new OpFOrdNotEqual(context.GetOrRegister(castTypeSameSize), context.Bound++, rowValue, context.CreateConstantCompositeRepeat(new FloatLiteral(new(32, true, true), 0.0, null, new()), elementSize).Id)),
                (ScalarType { TypeName: "int" }, ScalarType { TypeName: "bool" }) => InsertData(new OpINotEqual(context.GetOrRegister(castTypeSameSize), context.Bound++, rowValue, context.CreateConstantCompositeRepeat(new IntegerLiteral(new(32, false, true), 0, new()), elementSize).Id)),

                (ScalarType { TypeName: "int" }, ScalarType { TypeName: "float" }) => InsertData(new OpConvertSToF(context.GetOrRegister(castTypeSameSize), context.Bound++, rowValue)),

                (ScalarType { TypeName: "bool" }, ScalarType { TypeName: "int" }) => InsertData(new OpSelect(context.GetOrRegister(castTypeSameSize), context.Bound++, rowValue, context.CreateConstantCompositeRepeat(new IntegerLiteral(new(32, false, true), 1, new()), elementSize).Id, context.CreateConstant(new IntegerLiteral(new(32, false, true), 0, new())).Id)),
                (ScalarType { TypeName: "bool" }, ScalarType { TypeName: "float" }) => InsertData(new OpSelect(context.GetOrRegister(castTypeSameSize), context.Bound++, rowValue, context.CreateConstantCompositeRepeat(new FloatLiteral(new(32, true, true), 1.0, null, new()), elementSize).Id, context.CreateConstant(new FloatLiteral(new(32, true, true), 0.0, null, new())).Id)),
            };
            values[i] = typeCasting.IdResult!.Value;

            // Update type
            if (i == values.Length - 1)
                valueType = castTypeSameSize;
        }

        // Expanding
        int result = values[0];
        switch (valueType, castType)
        {
            case (ScalarType, VectorType v2):
                result = Insert(new OpCompositeConstruct(context.GetOrRegister(v2), context.Bound++, new LiteralArray<int>(Enumerable.Repeat(result, v2.Size).ToArray())));
                valueType = v2;
                break;
            case (ScalarType, MatrixType m2):
                result = Insert(new OpCompositeConstruct(context.GetOrRegister(new VectorType(m2.BaseType, m2.Columns)), context.Bound++, new LiteralArray<int>(Enumerable.Repeat(result, m2.Columns).ToArray())));
                result = Insert(new OpCompositeConstruct(context.GetOrRegister(m2), context.Bound++, new LiteralArray<int>(Enumerable.Repeat(result, m2.Rows).ToArray())));
                valueType = m2;
                break;
            case (MatrixType, MatrixType m2):
                // rebuild type
                result = Insert(new OpCompositeConstruct(context.GetOrRegister(m2), context.Bound++, new LiteralArray<int>(values)));
                valueType = m2;
                break;
        }

        if (valueType != castType)
            throw new NotImplementedException($"Type conversion from {originalType} to {castType} failed after expansion (current type: {valueType})");

        return new SpirvValue(result, context.GetOrRegister(castType));
    }

    public SpirvValue CallFunction(SymbolTable table, SpirvContext context, Symbol functionSymbol, Span<int> parameters)
    {
        // Note: Overload should have been chosen before
        if (functionSymbol.Type is FunctionGroupType)
            throw new InvalidOperationException();

        var functionType = (FunctionType)functionSymbol.Type;
        var fcall = Buffer.InsertData(Position++, new OpFunctionCall(context.GetOrRegister(functionType.ReturnType), context.Bound++, functionSymbol.IdRef, [.. parameters]));
        return new(fcall, functionSymbol.Id.Name);
    }

    public SpirvValue CompositeConstruct(SpirvContext context, CompositeLiteral literal, Span<int> values)
    {
        var instruction = Buffer.Insert(Position++, new OpCompositeConstruct(context.GetOrRegister(literal.Type), context.Bound++, [.. values]));
        return new(instruction.ResultId, instruction.ResultType);
    }
}




internal static class SymbolExtensions
{
    public static ScalarType GetElementType(this SymbolType symbol) => symbol switch
        {
            ScalarType s => s,
            VectorType v => v.BaseType,
            MatrixType m => m.BaseType,
        };
    public static bool IsSignedInteger(this SymbolType symbol) => symbol is ScalarType { TypeName: "sbyte" or "short" or "int" or "long" };
    public static bool IsUnsignedInteger(this SymbolType symbol) => symbol is ScalarType { TypeName: "byte" or "ushort" or "uint" or "ulong" };
    public static bool IsFloating(this SymbolType symbol) => symbol is ScalarType { TypeName: "half" or "float" or "double" };
    public static bool IsInteger(this SymbolType symbol) => symbol.IsSignedInteger() || symbol.IsUnsignedInteger();
    public static bool IsNumber(this SymbolType symbol) => symbol.IsInteger() || symbol.IsFloating();
    public static bool IsSigned(this SymbolType symbol) => symbol.IsSignedInteger() || symbol.IsFloating();
    public static bool IsUnsigned(this SymbolType symbol) => symbol.IsUnsignedInteger();
    public static bool IsSignedIntegerVector(this SymbolType symbol)
        => symbol.IsSignedInteger() || symbol is VectorType v && v.BaseType.IsSignedInteger();
    public static bool IsUnsignedIntegerVector(this SymbolType symbol)
        => symbol.IsUnsignedInteger() || symbol is VectorType v && v.BaseType.IsUnsignedInteger();
    public static bool IsIntegerVector(this SymbolType symbol)
        => symbol.IsInteger() || symbol is VectorType v && v.BaseType.IsInteger();
    public static bool IsFloatingVector(this SymbolType symbol)
        => symbol.IsFloating() || symbol is VectorType v && v.BaseType.IsFloating();
    public static bool IsNumberVector(this SymbolType symbol)
        => symbol.IsNumber() || symbol is VectorType v && v.BaseType.IsNumber();
    public static bool IsSignedVector(this SymbolType symbol)
        => symbol.IsSignedIntegerVector() || symbol.IsFloatingVector();
    public static bool IsUnsignedVector(this SymbolType symbol)
        => symbol.IsUnsignedIntegerVector();

    public static bool SameComponentCount(SymbolType left, SymbolType right)
        => (right, left) switch
        {
            (ScalarType l, ScalarType r) => true,
            (VectorType l, ScalarType r) => l.Size == 1,
            (ScalarType l, VectorType r) => r.Size == 1,
            (VectorType l, VectorType r) => l.Size == r.Size,
            (MatrixType l, VectorType r) => l.Columns == 1 && l.Rows == r.Size,
            (VectorType l, MatrixType r) => r.Columns == 1 && r.Rows == l.Size,
            (MatrixType l, MatrixType r) => r.Columns == l.Columns && r.Rows == l.Rows,
            _ => false
        };
    public static bool SameBaseTypeWidth(SymbolType left, SymbolType right)
        => (right, left) switch
        {
            (ScalarType { TypeName: "byte" or "sbyte" }, ScalarType { TypeName: "byte" or "sbyte" }) => true,
            (ScalarType { TypeName: "ushort" or "short" or "half" }, ScalarType { TypeName: "ushort" or "short" or "half" }) => true,
            (ScalarType { TypeName: "uint" or "int" or "float" }, ScalarType { TypeName: "uint" or "int" or "float" }) => true,
            (ScalarType { TypeName: "ulong" or "long" or "double" }, ScalarType { TypeName: "ulong" or "long" or "double" }) => true,
            (VectorType l, ScalarType r) => SameBaseType(l.BaseType, r),
            (ScalarType l, VectorType r) => SameBaseType(l, r.BaseType),
            (VectorType l, VectorType r) => SameBaseType(l.BaseType, r.BaseType),
            (MatrixType l, VectorType r) => SameBaseType(l.BaseType, r.BaseType),
            (VectorType l, MatrixType r) => SameBaseType(l.BaseType, r.BaseType),
            (MatrixType l, MatrixType r) => SameBaseType(l.BaseType, r.BaseType),
            _ => false
        };

    public static bool SameComponentCountAndWidth(SymbolType left, SymbolType right)
        => SameComponentCount(left, right) && SameBaseTypeWidth(left, right);
    public static bool SameBaseType(SymbolType left, SymbolType right)
        => (right, left) switch
        {
            (ScalarType l, ScalarType r) => l == r,
            (VectorType l, ScalarType r) => l.BaseType == r,
            (ScalarType l, VectorType r) => r.BaseType == l,
            (VectorType l, VectorType r) => l.BaseType == r.BaseType,
            (MatrixType l, VectorType r) => l.BaseType == r.BaseType,
            (VectorType l, MatrixType r) => l.BaseType == r.BaseType,
            (MatrixType l, MatrixType r) => l.BaseType == r.BaseType,
            _ => false
        };
    public static bool SameSignage(SymbolType left, SymbolType right)
        => (right, left) switch
        {
            (ScalarType l, ScalarType r) => l.IsInteger() && l.IsInteger(),
            (VectorType l, ScalarType r) => l.BaseType == r,
            (ScalarType l, VectorType r) => r.BaseType == l,
            (VectorType l, VectorType r) => l.BaseType == r.BaseType,
            (MatrixType l, VectorType r) => l.BaseType == r.BaseType,
            (VectorType l, MatrixType r) => l.BaseType == r.BaseType,
            (MatrixType l, MatrixType r) => l.BaseType == r.BaseType,
            _ => false
        };
}