using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Parsing.SDSL.AST;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public SpirvValue BinaryOperation(SpirvContext context, int resultType, in SpirvValue left, Operator op, in SpirvValue right, string? name = null)
    {

        var instruction = (op, context.ReverseTypes[left.TypeId], context.ReverseTypes[right.TypeId]) switch
        {
            (Operator.Plus, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpIAdd(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Plus, ScalarType l, ScalarType r)
                when l.IsFloatingVector() && r.IsFloatingVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpFAdd(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Minus, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpISub(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Minus, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpFSub(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Mul, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpIMul(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Mul, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpFMul(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Div, SymbolType l, SymbolType r)
                when l.IsUnsignedIntegerVector() && r.IsUnsignedIntegerVector() && SymbolExtensions.SameComponentCount(l, r)
                => Buffer.InsertOpUDiv(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Div, SymbolType l, SymbolType r)
                when l.IsIntegerVector() && r.IsIntegerVector() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpSDiv(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Div, SymbolType l, SymbolType r)
                when l.IsFloatingVector() && r.IsFloatingVector()
                => Buffer.InsertOpFDiv(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Mod, SymbolType l, SymbolType r)
                when l.IsUnsignedIntegerVector() && r.IsUnsignedIntegerVector()
                => Buffer.InsertOpUMod(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Mod, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger() && SymbolExtensions.SameComponentCountAndWidth(l, r)
                => Buffer.InsertOpSMod(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.Mod, SymbolType l, SymbolType r)
                when l.IsFloating() && r.IsNumber()
                => Buffer.InsertOpFMod(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.RightShift, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertOpShiftRightLogical(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.LeftShift, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertOpShiftRightLogical(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.AND, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertOpBitwiseAnd(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.OR, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertOpBitwiseOr(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.XOR, SymbolType l, SymbolType r)
                when l.IsInteger() && r.IsInteger()
                => Buffer.InsertOpBitwiseXor(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.LogicalAND, ScalarType { TypeName: "bool" }, ScalarType { TypeName: "bool" })
                => Buffer.InsertOpLogicalAnd(Position, context.Bound++, resultType, left.Id, right.Id),

            (Operator.LogicalOR, ScalarType { TypeName: "bool" }, ScalarType { TypeName: "bool" })
                => Buffer.InsertOpLogicalOr(Position, context.Bound++, resultType, left.Id, right.Id),

            _ => throw new NotImplementedException()
        };
        Position += instruction.WordCount;
        if (instruction.ResultId is int resultId)
        {
            if (name is not null)
                context.AddName(instruction, name);
            return new(instruction, name);
        }
        else throw new NotImplementedException("Instruction should have result id");
    }

    public SpirvValue CallFunction(SpirvContext context, string name, ReadOnlySpan<SpirvValue> parameters)
    {
        Span<IdRef> paramsIds = stackalloc IdRef[parameters.Length];
        var tmp = 0;
        foreach (var p in parameters)
            paramsIds[tmp++] = p.Id;
        return CallFunction(context, name, paramsIds);
    }
    public SpirvValue CallFunction(SpirvContext context, string name, Span<IdRef> parameters)
    {
        if (!context.Module.Functions.TryGetValue(name, out var func))
            context.Module.InheritedFunctions.TryGetValue(name, out func);

        var fcall = Buffer.InsertOpFunctionCall(Position, context.Bound++, context.GetOrRegister(func.FunctionType.ReturnType), func.Id, parameters);
        Position += fcall.WordCount;
        return new(fcall, func.Name);
    }

    public SpirvValue CompositeConstruct(SpirvContext context, CompositeLiteral literal, Span<IdRef> values)
    {
        var instruction = Buffer.InsertOpCompositeConstruct(Position, context.Bound++, context.GetOrRegister(literal.Type), values);
        Position += instruction.WordCount;
        return new(instruction);
    }
}




internal static class SymbolExtensions
{

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