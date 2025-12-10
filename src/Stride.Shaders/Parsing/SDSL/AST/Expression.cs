using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Text;

namespace Stride.Shaders.Parsing.SDSL.AST;



/// <summary>
/// Code expression, represents operations and literals
/// </summary>
public abstract class Expression(TextLocation info) : ValueNode(info)
{
    public SpirvValue Compile(SymbolTable table, CompilerUnit compiler)
    {
        var result = CompileImpl(table, compiler);
        // In case type is not computed yet, make sure it is using SpirvValue.TypeId
        Type ??= compiler.Context.ReverseTypes[result.TypeId];
        return result;
    }

    public abstract SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler);

    public SymbolType? ValueType { get => field ?? throw new InvalidOperationException($"Can't query {nameof(ValueType)} before calling {nameof(CompileAsValue)}"); private set; }

    public virtual SpirvValue CompileAsValue(SymbolTable table, CompilerUnit compiler)
    {
        var result = Compile(table, compiler);
        result = compiler.Builder.AsValue(compiler.Context, result);
        ValueType = compiler.Context.ReverseTypes[result.TypeId];
        return result;
    }
}

/// <summary>
/// Used only for <see cref="TypeName.ArraySize"/> when size is not explicitly defined.
/// </summary>
/// <param name="info"></param>
public class EmptyExpression(TextLocation info) : Expression(info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler) => throw new NotImplementedException();
    public override string ToString() => string.Empty;
}

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public SpirvValue? MemberCall { get; set; }
    public bool IsBaseCall { get; set; } = false;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        Symbol functionSymbol;
        if (MemberCall != null)
        {
            var type = (LoadedShaderSymbol)((PointerType)context.ReverseTypes[MemberCall.Value.TypeId]).BaseType;
            functionSymbol = type.Methods.Single(x => x.Symbol.Id.Name == Name).Symbol;
        }
        else
        {
            functionSymbol = table.ResolveSymbol(Name);
        }

        // TODO: find proper overload
        if (functionSymbol.Type is FunctionGroupType)
            functionSymbol = functionSymbol.GroupMembers.First();
        var functionType = (FunctionType)functionSymbol.Type;

        Type = functionType.ReturnType;

        var list = parameters.Values;

        Span<int> compiledParams = stackalloc int[list.Count];
        var tmp = 0;

        foreach (var p in list)
        {
            var paramSource = p.CompileAsValue(table, compiler);
            var paramType = functionType.ParameterTypes[tmp];

            // Wrap param in proper pointer type (function)
            var paramVariable = context.Bound++;
            builder.AddFunctionVariable(context.GetOrRegister(paramType), paramVariable);

            // Convert type (if necessary)
            var paramValueType = paramType;
            if (paramValueType is PointerType pointerType)
                paramValueType = pointerType.BaseType;
            paramSource = builder.Convert(context, paramSource, paramValueType);

            builder.Insert(new OpStore(paramVariable, paramSource.Id, null));

            compiledParams[tmp++] = paramVariable;
        }

        int? instance = null;
        if (MemberCall != null)
        {
            instance = MemberCall.Value.Id;
        }
        else if (IsBaseCall)
        {
            instance = builder.Insert(new OpBaseSDSL(context.Bound++)).ResultId;
        }
        else if (functionSymbol.MemberAccessWithImplicitThis is { } thisType)
        {
            var isStage = functionSymbol.Id.IsStage;
            instance = isStage
                ? builder.Insert(new OpStageSDSL(context.Bound++)).ResultId
                : builder.Insert(new OpThisSDSL(context.Bound++)).ResultId;
        }

        if (instance is int instanceId)
            functionSymbol.IdRef = builder.Insert(new OpMemberAccessSDSL(context.GetOrRegister(functionType), context.Bound++, instanceId, functionSymbol.IdRef)).ResultId;

        return builder.CallFunction(table, context, functionSymbol, [.. compiledParams]);
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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
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
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var expression = Expression.Compile(table, compiler);
        var type = Expression.Type;

        // Depending on the operator, we might need the pointer type
        var isPointer = Expression.Type is PointerType;

        var valueType = (Expression.Type is PointerType pointerType)
            ? pointerType.BaseType
            : type;

        var valueExpression = isPointer
            ? compiler.Builder.AsValue(compiler.Context, expression)
            : expression;

        if (valueType is ScalarType or VectorType)
        {
            switch (Operator)
            {
                case Operator.Inc:
                case Operator.Dec:
                    {
                        // Not supported yet
                        expression.ThrowIfSwizzle();
                        if (!isPointer)
                            throw new InvalidOperationException($"Can't use increment/decrement expression on non-pointer expression {Expression}");

                        // Use integer so that it gets converted to proper type according to expression type
                        var constant1 = context.CompileConstant(1);
                        var result = builder.BinaryOperation(context, valueExpression, Operator switch
                        {
                            Operator.Inc => Operator.Plus,
                            Operator.Dec => Operator.Minus,
                        }, constant1);

                        // We store the modified value back in the variable
                        builder.Insert(new OpStore(expression.Id, result.Id, null));

                        Type = type;
                        return expression;
                    }
                case Operator.Not:
                    {
                        if (valueType.GetElementType() is not ScalarType { TypeName: "bool" })
                            throw new InvalidOperationException();
                        var result = builder.Insert(new OpLogicalNot(valueExpression.TypeId, context.Bound++, valueExpression.Id));
                        Type = valueType;
                        return new(result.ResultId, result.ResultType);
                    }
                case Operator.Plus:
                    // Nothing to do
                    return expression;
                case Operator.Minus:
                    {
                        var result = valueType.GetElementType() switch
                        {
                            var elementType when elementType.IsFloating() => builder.InsertData(new OpFNegate(valueExpression.TypeId, context.Bound++, valueExpression.Id)),
                            var elementType when elementType.IsInteger() => builder.InsertData(new OpSNegate(valueExpression.TypeId, context.Bound++, valueExpression.Id)),
                        };
                        Type = valueType;
                        return new(result);
                    }
                default:
                    throw new NotImplementedException($"unary operator {Operator} is not implemented");
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}

public class CastExpression(TypeName typeName, Operator op, Expression expression, TextLocation info) : PrefixExpression(op, expression, info)
{
    public TypeName TypeName { get; set; } = typeName;

    public unsafe override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var castType = TypeName.ResolveType(table);
        var value = Expression.CompileAsValue(table, compiler);

        Type = castType;

        return builder.Convert(context, value, castType);
    }
}


public class IndexerExpression(Expression index, TextLocation info) : Expression(info)
{
    public Expression Index { get; set; } = index;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"[{Index}]";
    }
}

public class PostfixIncrement(Operator op, TextLocation info) : Expression(info)
{
    public Operator Operator { get; set; } = op;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        SpirvValue result;

        int firstIndex = 0;
        SymbolType currentValueType;
        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
        {
            result = streamVar.Compile(table, compiler);
            result.ThrowIfSwizzle();
            currentValueType = streamVar.Type;
            firstIndex = 1;
        }
        else if ((Source is Identifier { Name: "base" } || Source is Identifier { Name: "this" }) && Accessors[0] is MethodCall methodCall)
        {
            if (Source is Identifier { Name: "base" })
                methodCall.IsBaseCall = true;
            result = methodCall.Compile(table, compiler);
            result.ThrowIfSwizzle();
            currentValueType = methodCall.Type;
            firstIndex = 1;
        }
        else
        {
            result = Source.Compile(table, compiler);
            currentValueType = Source.Type;
        }
        if (Source is Identifier { Type: PointerType { BaseType: TextureType or Texture2DType or Texture3DType } } && Accessors is [MethodCall { Name.Name: "Sample", Parameters.Values.Count: 2 } or MethodCall { Name.Name: "SampleLevel", Parameters.Values.Count: 3 }])
        {
            result = Source.CompileAsValue(table, compiler);
            if (Accessors is [MethodCall { Name.Name: "Sample", Parameters.Values.Count: 2 } implicitSampling])
            {
                var textureValue = result;
                var samplerValue = implicitSampling.Parameters.Values[0].CompileAsValue(table, compiler);
                var texCoordValue = implicitSampling.Parameters.Values[1].CompileAsValue(table, compiler);
                var typeSampledImage = context.GetOrRegister(new SampledImage((TextureType)Source.ValueType));
                var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                var returnType = context.GetOrRegister(new VectorType(((TextureType)Source.ValueType).ReturnType, 4));
                var sample = builder.Insert(new OpImageSampleImplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, Specification.ImageOperandsMask.None));
                Type = ((TextureType)Source.ValueType).ReturnType;
                return new(sample.ResultId, sample.ResultType);
            }
            else if (Accessors is [MethodCall { Name.Name: "SampleLevel", Parameters.Values.Count: 3 } explicitSampling])
            {
                var textureValue = result;
                var samplerValue = explicitSampling.Parameters.Values[0].CompileAsValue(table, compiler);
                var texCoordValue = explicitSampling.Parameters.Values[1].CompileAsValue(table, compiler);
                var levelValue = explicitSampling.Parameters.Values[2].CompileAsValue(table, compiler);

                var typeSampledImage = context.GetOrRegister(new SampledImage((TextureType)Source.ValueType));
                var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                var returnType = context.GetOrRegister(new VectorType(((TextureType)Source.ValueType).ReturnType, 4));
                var sample = builder.Insert(new OpImageSampleExplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, ParameterizedFlags.ImageOperandsLod(levelValue.Id)));
                Type = ((TextureType)Source.ValueType).ReturnType;
                return new(sample.ResultId, sample.ResultType);
            }
            else
                throw new InvalidOperationException("Invalid Sample method call");
        }
        else
        {
            int accessChainIdCount = 0;
            void PushAccessChainId(Span<int> accessChainIds, int accessChainIndex)
            {
                accessChainIds[accessChainIdCount++] = accessChainIndex;
            }
            void EmitOpAccessChain(Span<int> accessChainIds)
            {
                // Do we need to issue an OpAccessChain?
                if (accessChainIdCount > 0)
                {
                    var resultType = context.GetOrRegister(currentValueType);
                    var test = new LiteralArray<int>(accessChainIds);
                    var accessChain = builder.Insert(new OpAccessChain(resultType, context.Bound++, result.Id, [.. accessChainIds.Slice(0, accessChainIdCount)]));
                    result = new SpirvValue(accessChain.ResultId, resultType) { Swizzles = result.Swizzles };
                }

                accessChainIdCount = 0;
            }

            Span<int> accessChainIds = stackalloc int[Accessors.Count];
            for (var i = firstIndex; i < Accessors.Count; i++)
            {
                var accessor = Accessors[i];
                switch (currentValueType, accessor)
                {
                    case (PointerType { BaseType: ShaderSymbol s }, MethodCall methodCall2):
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds);

                        methodCall2.MemberCall = result;
                        result = methodCall2.Compile(table, compiler);
                        break;
                    case (PointerType { BaseType: LoadedShaderSymbol s }, Identifier field):
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds);

                        if (!s.TryResolveSymbol(field.Name, out var matchingComponent))
                            throw new InvalidOperationException();

                        // TODO: figure out instance (this vs composition)
                        result = Identifier.EmitSymbol(compiler, builder, context, matchingComponent, result.Id);
                        accessor.Type = matchingComponent.Type;

                        break;
                    case (PointerType { BaseType: StructType s } p, Identifier field):

                        var index = s.TryGetFieldIndex(field);
                        if (index == -1)
                            throw new InvalidOperationException($"field {accessor} not found in struct type {s}");
                        //indexes[i] = builder.CreateConstant(context, shader, new IntegerLiteral(new(32, false, true), index, new())).Id;
                        PushAccessChainId(accessChainIds, context.CompileConstant(index).Id);
                        accessor.Type = new PointerType(s.Members[index].Type, p.StorageClass);
                        break;
                    // Swizzles
                    case (PointerType { BaseType: VectorType s } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                        if (swizzle.Length > 1)
                        {
                            Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                            for (int j = 0; j < swizzle.Length; ++j)
                                swizzleIndices[j] = ConvertSwizzle(swizzle[j]);

                            result.ApplySwizzles(swizzleIndices);

                            // Check resulting swizzles
                            for (int j = 0; j < result.Swizzles.Length; ++j)
                                if (swizzleIndices[j] >= s.Size)
                                    throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");

                            accessor.Type = currentValueType;
                        }
                        else
                        {
                            PushAccessChainId(accessChainIds, context.CompileConstant(ConvertSwizzle(swizzle[0])).Id);
                            accessor.Type = new PointerType(s.BaseType, p.StorageClass);
                        }
                        break;
                    case (VectorType v, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                        {
                            Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                            for (int j = 0; j < swizzle.Length; ++j)
                                swizzleIndices[j] = ConvertSwizzle(swizzle[j]);

                            (result, _) = builder.ApplyVectorSwizzles(context, result, v, swizzleIndices);
                            accessor.Type = v;

                            break;
                        }
                    case (PointerType { BaseType: ScalarType s } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                        if (swizzle.Length > 1)
                        {
                            Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                            for (int j = 0; j < swizzle.Length; ++j)
                                swizzleIndices[j] = ConvertSwizzle(swizzle[j]);

                            result.ApplySwizzles(swizzleIndices);
                            accessor.Type = currentValueType;
                        }
                        else
                        {
                            if (ConvertSwizzle(swizzle[0]) != 0)
                                throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");

                            // Do nothing
                            accessor.Type = currentValueType;
                        }
                        break;
                    case (ScalarType s, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                        if (swizzle.Length > 1)
                        {
                            Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                            for (int j = 0; j < swizzle.Length; ++j)
                            {
                                swizzleIndices[j] = ConvertSwizzle(swizzle[j]);
                                if (swizzleIndices[j] != 0)
                                    throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                            }

                            (result, _) = builder.ApplyScalarSwizzles(context, result, s, swizzleIndices);
                            accessor.Type = s;
                        }
                        else
                        {
                            // Do nothing
                            accessor.Type = currentValueType;
                        }
                        break;
                    // Array indexer for shader compositions
                    case (PointerType { BaseType: ArrayType { BaseType: ShaderSymbol s } }, IndexerExpression { Index: IntegerLiteral { Value: var compositionIndex } }):
                        break;
                    // Array indexer for arrays
                    case (PointerType { BaseType: ArrayType { BaseType: var t } } p, IndexerExpression indexer):
                        {
                            var indexerValue = indexer.Index.CompileAsValue(table, compiler);
                            PushAccessChainId(accessChainIds, indexerValue.Id);
                            accessor.Type = new PointerType(t, p.StorageClass);
                            break;
                        }
                    // Array indexer for vector/matrix
                    case (PointerType { BaseType: VectorType or MatrixType } p, IndexerExpression indexer):
                        {
                            var indexerValue = indexer.Index.CompileAsValue(table, compiler);
                            PushAccessChainId(accessChainIds, indexerValue.Id);

                            accessor.Type = new PointerType(p.BaseType switch
                            {
                                MatrixType m => new VectorType(m.BaseType, m.Rows),
                                VectorType v => v.BaseType,
                            }, p.StorageClass);
                            break;
                        }
                    case (PointerType { BaseType: var type }, PostfixIncrement postfix):
                        {
                            // Emit OpAccessChain with everything so far
                            EmitOpAccessChain(accessChainIds);

                            // Not supported yet
                            result.ThrowIfSwizzle();

                            var resultPointer = result;

                            // This is what this chain return (value before modification)
                            result = builder.AsValue(context, result);

                            // Use integer so that it gets converted to proper type according to expression type
                            var constant1 = context.CompileConstant(1);
                            var modifiedValue = builder.BinaryOperation(context, result, postfix.Operator switch
                            {
                                Operator.Inc => Operator.Plus,
                                Operator.Dec => Operator.Minus,
                            }, constant1);

                            // We store the modified value back in the variable
                            builder.Insert(new OpStore(resultPointer.Id, modifiedValue.Id, null));

                            break;
                        }
                    default:
                        throw new NotImplementedException($"unknown accessor {accessor} in expression {this}");
                }

                currentValueType = accessor.Type;
            }

            EmitOpAccessChain(accessChainIds);
        }

        Type = currentValueType;

        return result;
    }

    private static int ConvertSwizzle(char c)
        => c switch
        {
            'x' or 'r' => 0,
            'y' or 'g' => 1,
            'z' or 'b' => 2,
            'w' or 'a' => 3,
        };

    public override string ToString() => ToString(Accessors.Count);

    public string ToString(int accessorCount)
    {
        var builder = new StringBuilder().Append(Source);
        for (int i = 0; i < accessorCount; i++)
        {
            Expression? a = Accessors[i];
            if (a is IndexerExpression)
                builder.Append(a);
            else if (a is PostfixIncrement)
                builder.Append(a);
            else
                builder.Append('.').Append(a);
        }

        return builder.ToString();
    }
}

public class BinaryExpression(Expression left, Operator op, Expression right, TextLocation info) : Expression(info)
{
    public Operator Op { get; set; } = op;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var left = Left.CompileAsValue(table, compiler);
        var right = Right.CompileAsValue(table, compiler);

        var (builder, context) = compiler;
        var result = builder.BinaryOperation(context, left, Op, right);
        Type = context.ReverseTypes[result.TypeId];
        return result;
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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        Condition.CompileAsValue(table, compiler);
        Left.CompileAsValue(table, compiler);
        Right.CompileAsValue(table, compiler);
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