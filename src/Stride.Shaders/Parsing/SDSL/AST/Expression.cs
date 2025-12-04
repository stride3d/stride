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
    public abstract SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler);

    public SymbolType? ValueType => Type is PointerType pointerType ? pointerType.BaseType : Type;

    public virtual SpirvValue CompileAsValue(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var result = Compile(table, shader, compiler);
        return compiler.Builder.AsValue(compiler.Context, result);
    }
}

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public SpirvValue? MemberCall { get; set; }
    public bool IsBaseCall { get; set; } = false;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        Symbol functionSymbol;
        if (MemberCall != null)
        {
            var type = (ShaderSymbol)((PointerType)context.ReverseTypes[MemberCall.Value.TypeId]).BaseType;
            functionSymbol = type.Components.Single(x => x.Id.Name == Name);
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
            var paramSource = p.CompileAsValue(table, shader, compiler);
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
        else if (functionSymbol.ImplicitThisType is { } thisType)
        {
            var isStage = (functionSymbol.Id.FunctionFlags & Spirv.Specification.FunctionFlagsMask.Stage) != 0;
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
        var (builder, context) = compiler;
        var expression = Expression.CompileAsValue(table, shader, compiler);
        Type = Expression.Type;
        if (Expression.Type is PointerType pointerType && pointerType.BaseType is ScalarType { TypeName: "int" or "long" })
        {
            var constant1 = context.CompileConstant(1);
            var result = builder.BinaryOperation(context, expression, Operator.Plus, constant1);

            builder.Insert(new OpStore(expression.Id, result.Id, null));

            // Note: should we fetch the value again? (new OpLoad)
            // return Expression.Compile(table, shader, compiler);
            return result;
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

    public unsafe override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var castType = TypeName.ResolveType(table);
        var value = Expression.CompileAsValue(table, shader, compiler);

        Type = castType;

        return builder.Convert(context, value, castType);
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
        var (builder, context) = compiler;
        SpirvValue result;

        int firstIndex = 0;
        SymbolType currentValueType;
        if (Source is Identifier { Name: "streams" } streams && Accessors[0] is Identifier streamVar)
        {
            result = streamVar.Compile(table, shader, compiler);
            currentValueType = streamVar.Type;
            firstIndex = 1;
        }
        else if ((Source is Identifier { Name: "base" } || Source is Identifier { Name: "this" }) && Accessors[0] is MethodCall methodCall)
        {
            if (Source is Identifier { Name: "base" })
                methodCall.IsBaseCall = true;
            result = methodCall.Compile(table, shader, compiler);
            currentValueType = methodCall.Type;
            firstIndex = 1;
        }
        else
        {
            result = Source.Compile(table, shader, compiler);
            currentValueType = Source.Type;
        }
        if (Source is Identifier { ValueType: TextureType or Texture2DType or Texture3DType } && Accessors is [MethodCall { Name.Name: "Sample", Parameters.Values.Count: 2 } or MethodCall { Name.Name: "SampleLevel", Parameters.Values.Count: 3 }])
        {
            result = Source.CompileAsValue(table, shader, compiler);
            if (Accessors is [MethodCall { Name.Name: "Sample", Parameters.Values.Count: 2 } implicitSampling])
            {
                var textureValue = result;
                var samplerValue = implicitSampling.Parameters.Values[0].CompileAsValue(table, shader, compiler);
                var texCoordValue = implicitSampling.Parameters.Values[1].CompileAsValue(table, shader, compiler);
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
                var samplerValue = explicitSampling.Parameters.Values[0].CompileAsValue(table, shader, compiler);
                var texCoordValue = explicitSampling.Parameters.Values[1].CompileAsValue(table, shader, compiler);
                var levelValue = explicitSampling.Parameters.Values[2].CompileAsValue(table, shader, compiler);

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
                    result = new SpirvValue(accessChain.ResultId, resultType);
                }

                accessChainIdCount = 0;
            }

            Span<int> accessChainIds = stackalloc int[Accessors.Count];
            for (var i = firstIndex; i < Accessors.Count; i++)
            {
                var accessor = Accessors[i];
            ProcessAgain:
                switch (currentValueType, accessor)
                {
                    case (PointerType { BaseType: ShaderSymbol s }, MethodCall methodCall2):
                        // Emit OpAccessChain with everything so far
                        // next start is i + 1 because current value doesn't add a call
                        EmitOpAccessChain(accessChainIds);

                        methodCall2.MemberCall = result;
                        result = methodCall2.Compile(table, shader, compiler);
                        break;
                    case (PointerType { BaseType: ShaderSymbol s }, Identifier field):
                        // Emit OpAccessChain with everything so far
                        // next start is i + 1 because current value doesn't add a call
                        EmitOpAccessChain(accessChainIds);

                        if (!s.TryResolveSymbol(field.Name, out var matchingComponent))
                            throw new InvalidOperationException();

                        // TODO: figure out instance (this vs composition)
                        result = Identifier.EmitSymbol(compiler, builder, context, matchingComponent);
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
                            // For swizzle larger than one element, we need to do a OpLoad then do a OpVectorShuffle/OpCompositeExtract (next switch case)

                            // Emit OpAccessChain with everything so far
                            EmitOpAccessChain(accessChainIds);

                            var load = builder.InsertData(new OpLoad(context.GetOrRegister(s), context.Bound++, result.Id, null));
                            result = new(load);

                            currentValueType = s;

                            goto ProcessAgain;
                        }
                        else
                        {
                            PushAccessChainId(accessChainIds, context.CompileConstant(ConvertSwizzle(swizzle[0])).Id);
                            accessor.Type = new PointerType(s.BaseType, p.StorageClass);
                        }
                        break;
                    case (VectorType v, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():

                        Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                        for (int j = 0; j < swizzle.Length; ++j)
                            swizzleIndices[j] = ConvertSwizzle(swizzle[j]);

                        if (swizzle.Length > 1)
                        {
                            // Apply swizzle
                            var resultType = new VectorType(v.BaseType, swizzle.Length);
                            var shuffle = builder.InsertData(new OpVectorShuffle(context.GetOrRegister(resultType), context.Bound++, result.Id, result.Id, new(swizzleIndices)));
                            result = new(shuffle);

                            accessor.Type = resultType;
                        }
                        else if (swizzle.Length == 1)
                        {
                            // Apply swizzle
                            var resultType = v.BaseType;
                            var extract = builder.InsertData(new OpCompositeExtract(context.GetOrRegister(resultType), context.Bound++, result.Id, [context.CompileConstant(swizzleIndices[0]).Id]));
                            result = new(extract);

                            accessor.Type = resultType;
                        }
                        else
                            throw new InvalidOperationException();

                        break;
                    case (PointerType { BaseType: ScalarType s } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                        if (swizzle.Length > 1)
                        {
                            // For swizzle larger than one element, we need to do a OpLoad then do a OpVectorShuffle/OpCompositeExtract (next switch case)

                            // Emit OpAccessChain with everything so far
                            EmitOpAccessChain(accessChainIds);

                            var load = builder.InsertData(new OpLoad(context.GetOrRegister(s), context.Bound++, result.Id, null));
                            result = new(load);

                            currentValueType = s;

                            goto ProcessAgain;
                        }
                        else
                        {
                            // Do nothing
                            accessor.Type = currentValueType;
                        }
                        break;
                    case (ScalarType s, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                        if (swizzle.Length > 1)
                        {
                            var resultType = new VectorType(s, swizzle.Length);
                            Span<int> constructIndices = stackalloc int[swizzle.Length];
                            for (int j = 0; j < constructIndices.Length; ++j)
                                constructIndices[j] = result.Id;
                            var construct = builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(resultType), context.Bound++, new(constructIndices)));
                            result = new(construct);

                            accessor.Type = resultType;
                        }
                        else
                        {
                            // Do nothing
                            accessor.Type = currentValueType;
                        }
                        break;
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