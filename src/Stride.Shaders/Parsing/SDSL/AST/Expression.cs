using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Parsing.SDSL.AST;



/// <summary>
/// Code expression, represents operations and literals
/// </summary>
public abstract class Expression(TextLocation info) : ValueNode(info)
{
    public abstract SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler);

    public SymbolType? ValueType => Type is PointerType pointerType ? pointerType.BaseType : Type;

    public SpirvValue CompileAsValue(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var result = Compile(table, shader, compiler);
        var type = compiler.Context.ReverseTypes[result.TypeId];
        if (type is PointerType pointerType)
        {
            type = pointerType.BaseType;
            var inst = compiler.Builder.Insert(new OpLoad(compiler.Context.Types[type], compiler.Context.Bound++, result.Id, null));
            result = new(inst.ResultId, inst.ResultType);
        }
        return result;
    }
}

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var functionType = (FunctionType)Name.ResolveType(table);
        Type = functionType.ReturnType;

        var (builder, context, module) = compiler;
        var list = parameters.Values;
        Span<int> compiledParams = stackalloc int[list.Count];
        var tmp = 0;

        foreach (var p in list)
        {
            var paramSource = p.Compile(table, shader, compiler).Id;
            var paramType = context.GetOrRegister(functionType.ParameterTypes[tmp]);

            // Wrap param in proper pointer type (function)
            var paramVariable = context.Bound++;

            if (builder.CurrentFunction is SpirvFunction f)
            {
                var currentPosition = builder.Position;
                builder.SetPositionTo(f.BasicBlocks.First().Value, true);
                // Go after label
                builder.Position++;
                builder.Insert(new OpVariable(paramType, paramVariable, Specification.StorageClass.Function, null));

                builder.Position = currentPosition + 1;
            }

            var loadedParam = context.Bound++;
            builder.Insert(new OpLoad(compiler.Context.Types[p.ValueType], loadedParam, paramSource, null));
            builder.Insert(new OpStore(paramVariable, loadedParam, null));

            compiledParams[tmp++] = paramVariable;
        }

        return builder.CallFunction(context, Name, [.. compiledParams]);
    }
    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Parameters)})";
    }
}

/// <summary>
/// Represents an accessed mixin.
/// </summary>
public class MixinAccess(Mixin mixin, TextLocation info) : Expression(info)
{
    public Mixin Mixin { get; set; } = mixin;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"{Mixin}";
    }
}


public abstract class UnaryExpression(Expression expression, Operator op, TextLocation info) : Expression(info)
{
    public Expression Expression { get; set; } = expression;
    public Operator Operator { get; set; } = op;
}

public class PrefixExpression(Operator op, Expression expression, TextLocation info) : UnaryExpression(expression, op, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class CastExpression(string typeName, Operator op, Expression expression, TextLocation info) : PrefixExpression(op, expression, info)
{
    public string TypeName { get; set; } = typeName;
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}

public class PostfixIncrement(Operator op, TextLocation info) : Expression(info)
{
    public Operator Operator { get; set; } = op;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"{Operator.ToSymbol()}";
    }
}

public class AccessorChainExpression(Expression source, TextLocation info) : Expression(info)
{
    public Expression Source { get; set; } = source;
    public List<Expression> Accessors { get; set; } = [];

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, _) = compiler;
        SpirvValue source;
        var variable = context.Bound++;

        int firstIndex = 0;
        SymbolType currentValueType;
        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
        {
            source = streamVar.Compile(table, shader, compiler);
            currentValueType = streamVar.Type;
            firstIndex = 1;
        }
        else if ((Source is Identifier { Name: "base" } || Source is Identifier { Name: "this" }) && Accessors[0] is MethodCall methodCall)
        {
            source = methodCall.Compile(table, shader, compiler);
            currentValueType = methodCall.Type;
            firstIndex = 1;
        }
        else
        {
            source = Source.Compile(table, shader, compiler);
            currentValueType = Source.Type;
        }

        Span<int> indexes = stackalloc int[Accessors.Count];
        for (var i = firstIndex; i < Accessors.Count; i++)
        {
            var accessor = Accessors[i];
            if (currentValueType is PointerType p && p.BaseType is StructType s && accessor is Identifier field)
            {
                var index = s.TryGetFieldIndex(field);
                if (index == -1)
                    throw new InvalidOperationException($"field {accessor} not found in struct type {s}");
                //indexes[i] = builder.CreateConstant(context, shader, new IntegerLiteral(new(32, false, true), index, new())).Id;
                var indexLiteral = new IntegerLiteral(new(32, false, true), index, new());
                indexLiteral.Compile(table, shader, compiler);
                indexes[i] = context.CreateConstant(indexLiteral).Id;
            }
            else throw new NotImplementedException($"unknown accessor {accessor} in expression {this}");

            currentValueType = accessor.Type;
        }

        if (currentValueType is not PointerType)
            throw new InvalidOperationException();

        Type = currentValueType;

        // Do we need the OpAccessChain? (if we have streams.StreamVar, we can return StreamVar as is)
        if (firstIndex == Accessors.Count)
            return source;

        var resultType = context.GetOrRegister(Type);
        var result = builder.Insert(new OpAccessChain(variable, resultType, source.Id, [.. indexes]));
        return new(result.ResultId, resultType);
    }

    public override string ToString()
    {
        var builder = new StringBuilder().Append(Source);
        foreach (var a in Accessors)
            if (a is NumberLiteral)
                builder.Append('[').Append(a).Append(']');
            else if (a is PostfixIncrement)
                builder.Append(a);
            else
                builder.Append('.').Append(a);
        return builder.ToString();
    }
}

public class BinaryExpression(Expression left, Operator op, Expression right, TextLocation info) : Expression(info)
{
    public Operator Op { get; set; } = op;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var left = Left.CompileAsValue(table, shader, compiler);
        var right = Right.CompileAsValue(table, shader, compiler);
        if (
            OperatorTable.BinaryOperationResultingType(
                Left.ValueType ?? throw new NotImplementedException("Missing type"),
                Right.ValueType ?? throw new NotImplementedException("Missing type"),
                Op,
                out var t
            )
        )
            Type = t;
        else
            table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));

        var (builder, context, _) = compiler;
        return builder.BinaryOperation(context, context.GetOrRegister(Type), left, Op, right);
    }

    public override string ToString()
    {
        return $"( {Left} {Op.ToSymbol()} {Right} )";
    }
}

public class TernaryExpression(Expression cond, Expression left, Expression right, TextLocation info) : Expression(info)
{
    public Expression Condition { get; set; } = cond;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Condition.CompileAsValue(table, shader, compiler);
        Left.CompileAsValue(table, shader, compiler);
        Right.CompileAsValue(table, shader, compiler);
        if (Condition.ValueType is not ScalarType { TypeName: "bool" })
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));
        if (Left.ValueType != Right.ValueType)
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"({Condition} ? {Left} : {Right})";
    }
}