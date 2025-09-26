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
                => Buffer.InsertData(Position++, new OpIAdd(resultType, context.Bound++, left.Id, right.Id)),

            (Operator.Plus, ScalarType l, ScalarType r)
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

            _ => throw new NotImplementedException()
        };

        if (name is not null)
            context.AddName(instruction.IdResult ?? -1, name);
        return new(instruction, name);
    }

    public SpirvValue CallFunction(SpirvContext context, string name, ReadOnlySpan<SpirvValue> parameters)
    {
        Span<int> paramsIds = stackalloc int[parameters.Length];
        var tmp = 0;
        foreach (var p in parameters)
            paramsIds[tmp++] = p.Id;
        return CallFunction(context, name, [.. paramsIds]);
    }
    public SpirvValue CallFunction(SpirvContext context, string name, Span<int> parameters)
    {
        var func = FindFunction(context, name);

        var fcall = Buffer.InsertData(Position++, new OpFunctionCall(context.GetOrRegister(func.FunctionType.ReturnType), context.Bound++, func.Id, [.. parameters]));
        return new(fcall, func.Name);
    }

    private static SpirvFunction FindFunction(SpirvContext context, string name)
    {
        if (!context.Module.Functions.TryGetValue(name, out var func))
            context.Module.InheritedFunctions.TryGetValue(name, out func);
        return func;
    }

    public SpirvValue CompositeConstruct(SpirvContext context, CompositeLiteral literal, Span<int> values)
    {
        var instruction = Buffer.Insert(Position++, new OpCompositeConstruct(context.GetOrRegister(literal.Type), context.Bound++, [.. values]));
        return new(instruction.ResultId, instruction.ResultType);
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