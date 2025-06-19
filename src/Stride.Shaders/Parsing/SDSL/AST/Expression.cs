using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
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
}

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public override void ProcessSymbol(SymbolTable table)
    {
        foreach (var p in parameters.Values)
            p.ProcessSymbol(table);
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, module) = compiler;
        var list = parameters.Values;
        Span<IdRef> compiledParams = stackalloc IdRef[list.Count];
        var tmp = 0;
        foreach(var p in list)
            compiledParams[tmp++] = p.Compile(table, shader, compiler).Id;
        return builder.CallFunction(context, Name, compiledParams);
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

    public override void ProcessSymbol(SymbolTable table)
    {
        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
        {
            //table.CurrentSymbols.Add(table.Streams);
            streamVar.ProcessSymbol(table);
            Type = streamVar.Type;

            if (Accessors.Count > 1)
                ProcessAccessors(1);
        }
        else if ((Source is Identifier { Name: "base" } || Source is Identifier { Name: "this" }) && Accessors[0] is MethodCall methodCall)
        {
            methodCall.ProcessSymbol(table);
            Type = methodCall.Type;

            if (Accessors.Count > 1)
                ProcessAccessors(1);
        }
        else
        {
            Source.ProcessSymbol(table);
            Type = Source.Type;
            ProcessAccessors(0);
        }

        // AccessorChain always end up with a pointer type
        Type = new PointerType(Type);

        void ProcessAccessors(int firstIndex)
        {
            foreach (var accessor in Accessors[firstIndex..])
            {
                if (Type is not null && Type.TryAccess(accessor, out var type))
                {
                    Type = type;
                    accessor.Type = type;
                }
                else throw new NotImplementedException($"Cannot access {accessor.GetType().Name} from {Type}");

                if(accessor is not Identifier)
                    accessor.ProcessSymbol(table);
            }
        }
    }
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, _) = compiler;
        SpirvValue source;
        var variable = context.Bound++;

        int firstIndex = 0;
        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
        {
            source = streamVar.Compile(table, shader, compiler);
            firstIndex = 1;
        }
        else
        {
            source = Source.Compile(table, shader, compiler);
        }

        var currentValueType = Source.Type;
        Span<IdRef> indexes = stackalloc IdRef[Accessors.Count];
        for (var i = firstIndex; i < Accessors.Count; i++)
        {
            var accessor = Accessors[i];
            if (currentValueType is StructType s && accessor is Identifier field)
            {
                var index = s.TryGetFieldIndex(field);
                if (index == -1)
                    throw new InvalidOperationException($"field {accessor} not found in struct type {s}");
                //indexes[i] = builder.CreateConstant(context, shader, new IntegerLiteral(new(32, false, true), index, new())).Id;
                var indexLiteral = new IntegerLiteral(new(32, false, true), index, new());
                indexLiteral.ProcessSymbol(table);
                indexes[i] = context.CreateConstant(indexLiteral).Id;
            }
            else throw new NotImplementedException($"unknown accessor {accessor} in expression {this}");

            currentValueType = accessor.Type;
        }

        // Do we need the OpAccessChain? (if we have streams.StreamVar, we can return StreamVar as is)
        if (firstIndex == Accessors.Count)
            return source;

        var resultType = context.GetOrRegister(Type);
        var result = builder.Buffer.InsertOpAccessChain(builder.Position++, variable, resultType, source.Id, indexes);
        return new(result, resultType);
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

    public override void ProcessSymbol(SymbolTable table)
    {
        Left.ProcessSymbol(table);
        Right.ProcessSymbol(table);
        if (
            OperatorTable.BinaryOperationResultingType(
                Left.Type ?? throw new NotImplementedException("Missing type"),
                Right.Type ?? throw new NotImplementedException("Missing type"),
                Op,
                out var t
            )
        )
            Type = t;
        else
            table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var left = Left.Compile(table, shader, compiler);
        var right = Right.Compile(table, shader, compiler);
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

    public override void ProcessSymbol(SymbolTable table)
    {
        Condition.ProcessSymbol(table);
        Left.ProcessSymbol(table);
        Right.ProcessSymbol(table);
        if (Condition.Type is not ScalarType { TypeName: "bool" })
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));
        if (Left.Type != Right.Type)
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException(); 
    }

    public override string ToString()
    {
        return $"({Condition} ? {Left} : {Right})";
    }
}