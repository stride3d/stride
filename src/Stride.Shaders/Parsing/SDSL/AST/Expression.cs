using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Text;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;



/// <summary>
/// Code expression, represents operations and literals
/// </summary>
public abstract class Expression(TextLocation info) : ValueNode(info)
{
    public SpirvValue Compile(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var result = CompileImpl(table, compiler, expectedType);
        // In case type is not computed yet, make sure it is using SpirvValue.TypeId
        if (result.TypeId != 0)
            Type ??= compiler.Context.ReverseTypes[result.TypeId];
        return result;
    }

    /// <summary>
    /// Assign to l-value. 
    /// </summary>
    public virtual void SetValue(SymbolTable table, CompilerUnit compiler, SpirvValue rvalue)
    {
        ThrowErrorOnLValue();
    }

    protected void ThrowErrorOnLValue()
    {
        throw new InvalidOperationException($"{this} is not a l-value and cannot be assigned to.");
    }

    public abstract SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null);

    public SymbolType? ValueType { get => field ?? throw new InvalidOperationException($"Can't query {nameof(ValueType)} before calling {nameof(CompileAsValue)}"); private set; }

    public virtual SpirvValue CompileAsValue(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var result = Compile(table, compiler, expectedType);
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
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null) => throw new NotImplementedException();
    public override string ToString() => string.Empty;
}

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public SpirvValue? MemberCall { get; set; }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;

        var functionSymbol = ResolveFunctionSymbol(table, context);
        var functionType = (FunctionType)functionSymbol.Type;

        Type = functionType.ReturnType;

        Span<int> compiledParams = stackalloc int[functionType.ParameterTypes.Count];
        
        if (parameters.Values.Count > functionType.ParameterTypes.Count)
            throw new InvalidOperationException($"Function {Name} was called with {parameters.Values.Count} arguments but only {functionType.ParameterTypes.Count} expected");

        for (int i = 0; i < parameters.Values.Count; i++)
        {
            // Wrap param in proper pointer type (function)
            var paramDefinition = functionType.ParameterTypes[i];
            var paramVariable = context.Bound++;
            builder.AddFunctionVariable(context.GetOrRegister(paramDefinition.Type), paramVariable);

            // Note: "in" is implicit, so we match in all cases except if out
            var inOutFlags = paramDefinition.Modifiers & ParameterModifiers.InOut;
            if (inOutFlags != ParameterModifiers.Out)
            {
                var paramSource = parameters.Values[i].CompileAsValue(table, compiler);

                // Convert type (if necessary)
                var paramExpectedValueType = paramDefinition.Type;
                if (paramExpectedValueType is PointerType pointerType)
                    paramExpectedValueType = pointerType.BaseType;
                paramSource = builder.Convert(context, paramSource, paramExpectedValueType);

                builder.Insert(new OpStore(paramVariable, paramSource.Id, null, []));
            }

            compiledParams[i] = paramVariable;
        }
        
        // Find default parameters decoration (if any)
        var missingParameters = functionType.ParameterTypes.Count - parameters.Values.Count;
        var defaultParameters = 0;
        if (missingParameters > 0 && functionSymbol.MethodDefaultParameters is {} methodDefaultParameters)
        {
            // Is there enough parameters now?
            if (missingParameters <= methodDefaultParameters.DefaultValues.Length)
            {
                // Import missing parameters
                for (int i = 0; i < missingParameters; ++i)
                {
                    var paramDefinition = functionType.ParameterTypes[parameters.Values.Count + i];

                    var source = methodDefaultParameters.DefaultValues[^(missingParameters - i)];
                    // Import in current buffer
                    if (methodDefaultParameters.SourceContext != context)
                    {
                        var bufferForConstant = methodDefaultParameters.SourceContext.ExtractConstantAsSpirvBuffer(source);
                        source = context.InsertWithoutDuplicates(null, bufferForConstant);
                    }
                    
                    var paramVariable = context.Bound++;
                    builder.AddFunctionVariable(context.GetOrRegister(paramDefinition.Type), paramVariable);
                    builder.Insert(new OpStore(paramVariable, source, null, []));

                    compiledParams[parameters.Values.Count + i] = paramVariable;
                }
                missingParameters = 0;
            }
        }
        
        if (missingParameters > 0)
            throw new InvalidOperationException($"Function {Name} was called with {parameters.Values.Count} arguments but there was {(defaultParameters > 0 ? $"between {functionType.ParameterTypes.Count - defaultParameters} and {functionType.ParameterTypes.Count}" : functionType.ParameterTypes.Count)} expected");

        int? instance = null;
        if (MemberCall != null)
        {
            instance = MemberCall.Value.Id;
        }
        else if (functionSymbol.MemberAccessWithImplicitThis is { } thisType)
        {
            instance = builder.Insert(new OpThisSDSL(context.Bound++)).ResultId;
        }

        if (instance is int instanceId)
            functionSymbol.IdRef = builder.Insert(new OpMemberAccessSDSL(context.GetOrRegister(functionType), context.Bound++, instanceId, functionSymbol.IdRef)).ResultId;
        
        var result = builder.CallFunction(table, context, functionSymbol, [.. compiledParams]);

        for (int i = 0; i < parameters.Values.Count; i++)
        {
            var paramDefinition = functionType.ParameterTypes[i];
            if (paramDefinition.Modifiers.HasFlag(ParameterModifiers.Out))
            {
                var paramDefinitionType = (PointerType)paramDefinition.Type;
                var paramVariable = compiledParams[i];
                var paramTarget = parameters.Values[i].Compile(table, compiler);
                var paramTargetType = (PointerType)context.ReverseTypes[paramTarget.TypeId];

                if (paramTargetType.BaseType != paramDefinitionType.BaseType)
                    throw new InvalidOperationException($"Value of type {paramTargetType.BaseType} can't be used as out parameter {i} of type {paramDefinitionType.BaseType} in method call [{this}]");

                var loadedResult = builder.Insert(new OpLoad(context.GetOrRegister(paramTargetType.BaseType), context.Bound++, paramVariable, null, [])).ResultId;
                builder.Insert(new OpStore(paramTarget.Id, loadedResult, null, []));
            }
        }

        return result;
    }

    private Symbol ResolveFunctionSymbol(SymbolTable table, SpirvContext context)
    {
        Symbol functionSymbol;
        // Note: for now, TypeId 0 is used for this/base; let's improve that later
        if (MemberCall != null && MemberCall.Value.TypeId != 0)
        {
            var type = (LoadedShaderSymbol)((PointerType)context.ReverseTypes[MemberCall.Value.TypeId]).BaseType;
            if (!type.TryResolveSymbol(table, context, Name, out functionSymbol))
                throw new InvalidOperationException($"Method {Name} could not be found in type {type.Name}");
        }
        else
        {
            functionSymbol = table.ResolveSymbol(Name);
        }

        // Choose appropriate method to call
        if (functionSymbol.Type is FunctionGroupType)
        {
            // Find methods matching number of parameters
            var matchingMethods = functionSymbol.GroupMembers.Where(x => ((FunctionType)x.Type).ParameterTypes.Count == parameters.Values.Count);

            // TODO: find proper overload (different signature)
            // We take first element, so in case there is multiple override, it will take the most-derived implementation
            // Note: this will be reevaluted during ShaderMixer (base/this, etc.) but it won't change overload (different signature)
            functionSymbol = matchingMethods.First();
        }

        return functionSymbol;
    }

    private Symbol ResolveSymbol(SymbolTable table, SpirvContext context)
    {
        Symbol functionSymbol;
        if (MemberCall != null)
        {
            var type = (LoadedShaderSymbol)((PointerType)context.ReverseTypes[MemberCall.Value.TypeId]).BaseType;
            if (!type.TryResolveSymbol(table, context, Name, out functionSymbol))
                throw new InvalidOperationException($"Method {Name} could not be found in type {type.Name}");
        }
        else
        {
            functionSymbol = table.ResolveSymbol(Name);
        }

        return functionSymbol;
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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        
        // MixinAccess is same as Identifier static variable case, except we have generics (which is why MixinAccess was chosen over Identifier)
        var generics = SDFX.AST.ShaderEffect.CompileGenerics(table, compiler, Mixin.Generics);
        var classSource = new ShaderClassInstantiation(Mixin.Name, generics);
        if (!table.TryResolveSymbol(classSource.ToClassNameWithGenerics(), out var symbol))
        {
            if (!table.ShaderLoader.Exists(classSource.ClassName))
                throw new InvalidOperationException($"Symbol [{classSource.ClassName}] could not be found.");

            // Shader is inherited (TODO: do we want to do something more "selective", i.e. import only the required variable if it's a cbuffer?)
            var inheritedShaderCount = table.InheritedShaders.Count;
            classSource = SpirvBuilder.BuildInheritanceListIncludingSelf(table.ShaderLoader, context, classSource, table.CurrentMacros.AsSpan(), table.InheritedShaders, ResolveStep.Compile);
            for (int i = inheritedShaderCount; i < table.InheritedShaders.Count; ++i)
            {
                table.InheritedShaders[i].Symbol = ShaderClass.LoadAndCacheExternalShaderType(table, context, table.InheritedShaders[i]);
                ShaderClass.Inherit(table, context, table.InheritedShaders[i].Symbol, false);
            }

            // We add the typename as a symbol (similar to static access in C#)
            var shaderId = context.GetOrRegister(classSource.Symbol);
            symbol = new Symbol(new(classSource.Symbol.Name, SymbolKind.Shader), new PointerType(classSource.Symbol, Specification.StorageClass.Private), shaderId);
            table.CurrentFrame.Add(classSource.ToClassNameWithGenerics(), symbol);
        }

        Type = symbol.Type;
        return Identifier.EmitSymbol(builder, context, symbol, builder.CurrentFunction == null);
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
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
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
                        builder.Insert(new OpStore(expression.Id, result.Id, null, []));

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

    public unsafe override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var castType = TypeName.ResolveType(table, context);
        var value = Expression.CompileAsValue(table, compiler);

        Type = castType;

        return builder.Convert(context, value, castType);
    }
}


public class IndexerExpression(Expression index, TextLocation info) : Expression(info)
{
    public Expression Index { get; set; } = index;

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
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

    private SpirvValue[] intermediateValues;

    public override void SetValue(SymbolTable table, CompilerUnit compiler, SpirvValue rvalue)
    {
        var (builder, context) = compiler;

        // Compute the l-value (and all its intermediate values)
        CompileHelper(table, compiler, null);
        
        // Only things left should be:
        // - RWBuffer/Texture setters
        // - Swizzles
        
        // Process from end
        for (var i = Accessors.Count - 1; i >= 0; --i)
        {
            var accessor = Accessors[i];
            var currentValueType = i > 0 ? Accessors[i - 1].Type : Source.Type;
            var resultValueType = Accessors[i].Type;
            var lvalueBase = intermediateValues[1 + i - 1];
            var lvalueResult = intermediateValues[1 + i];

            // if lvalue is a pointer, we can simply assign to it
            if (resultValueType is PointerType)
            {
                var expectedType = (PointerType)context.ReverseTypes[lvalueResult.TypeId];
                rvalue = builder.Convert(context, rvalue, expectedType.BaseType);
                builder.Insert(new OpStore(lvalueResult.Id, rvalue.Id, null, []));
                return;
            }
            
            switch (currentValueType, accessor)
            {
                case (PointerType { BaseType: BufferType bufferType }, IndexerExpression indexer):
                    throw new NotImplementedException();
                // ImageWrite
                case (PointerType { BaseType: TextureType textureType }, IndexerExpression indexer):
                    var resultType = new VectorType(textureType.ReturnType, 4);

                    var imageValue = builder.AsValue(context, lvalueBase);
                    var imageCoordValue = ConvertTexCoord(context, builder, textureType, indexer.Index.CompileAsValue(table, compiler), ScalarType.From("int"));
                    var texelValue = builder.Convert(context, rvalue, resultType);
                    builder.Insert(new OpImageWrite(imageValue.Id, imageCoordValue.Id, texelValue.Id, null, []));
                    // We stop there
                    return;
                case (PointerType { BaseType: VectorType or ScalarType } or VectorType or ScalarType, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    // Swizzle: we transform the value to assign accordingly
                    
                    // We load the original value (if pointer)
                    var lvalueType = currentValueType;
                    if (lvalueType is PointerType p)
                    {
                        lvalueBase = new(builder.InsertData(new OpLoad(context.GetOrRegister(lvalueType), context.Bound++, lvalueBase.Id, null, [])));
                        lvalueType = p.BaseType;
                    }
                    
                    // Shuffle with new data
                    switch (lvalueType)
                    {
                        case VectorType v:
                            Span<int> shuffleIndices = stackalloc int[v.Size];
                            // Default: lvalue
                            for (int j = 0; j < v.Size; ++j)
                                shuffleIndices[j] = j;
                            // Update using swizzle target (from 2nd new value vector)
                            for (int j = 0; j < swizzle.Length; ++j)
                                shuffleIndices[ConvertSwizzle(swizzle[j])] = v.Size + j;
                            // Compute the rvalue at this step (by possibly combining with lvalue if not writing every component)
                            rvalue = new(builder.InsertData(new OpVectorShuffle(context.GetOrRegister(lvalueType), context.Bound++, lvalueBase.Id, rvalue.Id, new(shuffleIndices))));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
            }
        }
        
        // Need to assign to Source
        if (Source.Type is PointerType expectedType2)
        {
            rvalue = builder.Convert(context, rvalue, expectedType2.BaseType);
            builder.Insert(new OpStore(intermediateValues[0].Id, rvalue.Id, null, []));
            return;
        }
        
        // We should not reach this point (unless we can't write back to lvalue)
        ThrowErrorOnLValue();
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        return CompileHelper(table, compiler, expectedType);
    }

    public SpirvValue CompileHelper(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType)
    {
        if (intermediateValues != null)
            return intermediateValues[^1];

        intermediateValues = new SpirvValue[Accessors.Count + 1];
        
        var (builder, context) = compiler;
        SpirvValue result;

        result = Source.Compile(table, compiler);
        var currentValueType = Source.Type;
        intermediateValues[0] = result;

        int accessChainIdCount = 0;
        void PushAccessChainId(Span<int> accessChainIds, int accessChainIndex)
        {
            accessChainIds[accessChainIdCount++] = accessChainIndex;
        }
        
        void EmitOpAccessChain(Span<int> accessChainIds, int i)
        {
            // Do we need to issue an OpAccessChain?
            if (accessChainIdCount > 0)
            {
                var resultType = context.GetOrRegister(currentValueType);
                var test = new LiteralArray<int>(accessChainIds);
                var accessChain = builder.Insert(new OpAccessChain(resultType, context.Bound++, result.Id, [.. accessChainIds.Slice(0, accessChainIdCount)]));
                result = new SpirvValue(accessChain.ResultId, resultType);
                
                intermediateValues[1 + i] = result;
            }

            accessChainIdCount = 0;
        }

        // If current and next accessors are swizzle, combine them
        void CoalesceSwizzles(int i, SymbolType currentValueType, ref Expression accessor)
        {
            if (i + 1 < Accessors.Count
                && currentValueType is PointerType { BaseType: VectorType or ScalarType } or VectorType or ScalarType
                && Accessors[i] is Identifier { Name: var swizzle1 } id1 && id1.IsVectorSwizzle()
                && Accessors[i + 1] is Identifier { Name: var swizzle2 } id2 && id2.IsVectorSwizzle())
            {
                var vectorOrScalarType = currentValueType is PointerType p ? p.BaseType : currentValueType; 
                    
                (var size, ScalarType baseType) = vectorOrScalarType switch
                {
                    ScalarType s => (1, s),
                    VectorType v => (v.Size, v.BaseType),
                };
                
                var swizzleIndices = new int[swizzle1.Length];
                for (int j = 0; j < swizzle1.Length; ++j)
                {
                    swizzleIndices[j] = ConvertSwizzle(swizzle1[j]);
                    if (swizzleIndices[j] >= size)
                        throw new InvalidOperationException($"Swizzle {Accessors[i]} is out of bound for expression {ToString(i)} of type {vectorOrScalarType}");
                }
                
                // Combine swizzles with previous ones
                var newSwizzleIndices = new int[swizzle2.Length];
                for (int j = 0; j < swizzle2.Length; ++j)
                {
                    newSwizzleIndices[j] = swizzleIndices[ConvertSwizzle(swizzle2[j])];
                    if (newSwizzleIndices[j] >= size)
                        throw new InvalidOperationException($"Swizzle {Accessors[i + 1]} is out of bound for expression {ToString(i)} of type {currentValueType}");
                }

                Accessors.RemoveAt(i + 1);
                Span<char> vectorFields = ['x', 'y', 'z', 'w'];
                Span<char> newSwizzle = stackalloc char[swizzle2.Length];
                for (int j = 0; j < swizzle2.Length; ++j)
                {
                    newSwizzle[j] = vectorFields[newSwizzleIndices[j]];
                }

                Accessors[i] = accessor = new Identifier(new(newSwizzle), default);
            }
        }

        // Some accessors push up to 2 values on the stack
        Span<int> accessChainIds = stackalloc int[Accessors.Count * 2];
        
        for (var i = 0; i < Accessors.Count; ++i)
        {
            var accessor = Accessors[i];

            CoalesceSwizzles(i, currentValueType, ref accessor);
            
            switch (currentValueType, accessor)
            {
                case (PointerType { BaseType: TextureType textureType },
                        MethodCall { Name.Name: "Sample", Parameters.Values.Count: 2 or 3 }
                        or MethodCall { Name.Name: "SampleLevel", Parameters.Values.Count: 3 or 4 }
                        or MethodCall { Name.Name: "SampleCmp" or "SampleCmpLevelZero", Parameters.Values.Count: 3 or 4 }):
                    {
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds, i - 1);
                        var textureValue = builder.AsValue(context, result);

                        if (accessor is MethodCall { Name.Name: "Sample", Parameters.Values.Count: 2 or 3 } implicitSampling)
                        {
                            var resultType = new VectorType(textureType.ReturnType, 4);

                            var samplerValue = implicitSampling.Parameters.Values[0].CompileAsValue(table, compiler);
                            var texCoordValue = ConvertTexCoord(context, builder, textureType, implicitSampling.Parameters.Values[1].CompileAsValue(table, compiler), ScalarType.From("float"));

                            var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
                            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                            var returnType = context.GetOrRegister(resultType);

                            ImageOperandsMask? imask = null;
                            EnumerantParameters imParams = [];
                            if (implicitSampling.Parameters.Values.Count > 2)
                            {
                                var offset = ConvertOffset(context, builder, textureType, implicitSampling.Parameters.Values[2].CompileAsValue(table, compiler));
                                // TODO: determine when ConstOffset
                                imask = ImageOperandsMask.Offset;
                                imParams = new EnumerantParameters(offset.Id);
                            }
                            var sample = builder.Insert(new OpImageSampleImplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, imask, imParams));

                            result = new(sample.ResultId, sample.ResultType);
                            accessor.Type = resultType;
                        }
                        else if (accessor is MethodCall { Name.Name: "SampleLevel", Parameters.Values.Count: 3 or 4 } explicitSampling)
                        {
                            var resultType = new VectorType(textureType.ReturnType, 4);

                            var samplerValue = explicitSampling.Parameters.Values[0].CompileAsValue(table, compiler);
                            var texCoordValue = ConvertTexCoord(context, builder, textureType, explicitSampling.Parameters.Values[1].CompileAsValue(table, compiler), ScalarType.From("float"));
                            
                            var levelValue = explicitSampling.Parameters.Values[2].CompileAsValue(table, compiler);
                            levelValue = builder.Convert(context, levelValue, ScalarType.From("float"));

                            var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
                            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                            var returnType = context.GetOrRegister(resultType);

                            ImageOperandsMask imask = ImageOperandsMask.None;
                            EnumerantParameters imParams = [];

                            if (explicitSampling.Parameters.Values.Count > 3)
                            {
                                var offset = ConvertOffset(context, builder, textureType, explicitSampling.Parameters.Values[3].CompileAsValue(table, compiler));
                                // TODO: determine when ConstOffset
                                imask = ImageOperandsMask.Lod | ImageOperandsMask.Offset;
                                imParams = new EnumerantParameters(levelValue.Id, offset.Id);
                            }
                            else
                            {
                                imask = ImageOperandsMask.Lod;
                                imParams = new EnumerantParameters(levelValue.Id);
                            }
                            var sample = builder.Insert(new OpImageSampleExplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, imask, imParams));

                            result = new(sample.ResultId, sample.ResultType);
                            accessor.Type = resultType;
                        }
                        else if (accessor is MethodCall { Name.Name: "SampleCmp" or "SampleCmpLevelZero", Parameters.Values.Count: 3 or 4 } sampleCompare)
                        {
                            var resultType = textureType.ReturnType;
                            if (resultType is not ScalarType)
                                throw new InvalidOperationException();

                            var samplerValue = sampleCompare.Parameters.Values[0].CompileAsValue(table, compiler);
                            var texCoordValue = ConvertTexCoord(context, builder, textureType, sampleCompare.Parameters.Values[1].CompileAsValue(table, compiler), ScalarType.From("float"));
                            
                            var compareValue = sampleCompare.Parameters.Values[2].CompileAsValue(table, compiler);

                            var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
                            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                            var returnType = context.GetOrRegister(resultType);


                            ImageOperandsMask flags = sampleCompare.Name.Name is "SampleCmpLevelZero" ? ImageOperandsMask.Lod : ImageOperandsMask.None;
                            EnumerantParameters imParams = sampleCompare.Name.Name is "SampleCmpLevelZero" ? new () : new (context.CompileConstant(0.0f).Id);
                            //ParameterizedFlag<ImageOperandsMask> flags = sampleCompare.Name.Name == "SampleCmpLevelZero" 
                                
                            if (sampleCompare.Parameters.Values.Count > 3)
                            {
                                var offset = ConvertOffset(context, builder, textureType, sampleCompare.Parameters.Values[3].CompileAsValue(table, compiler));
                                // TODO: determine when ConstOffset
                                flags |= ImageOperandsMask.Offset;
                                imParams = new EnumerantParameters([..imParams, offset.Id]);
                                //flags = new ParameterizedFlag<ImageOperandsMask>(flags | ImageOperandsMask.Offset, new(..imParams.Span, offset.Id]);
                            }

                            var sample = sampleCompare.Name.Name == "SampleCmpLevelZero"
                                ? builder.InsertData(new OpImageSampleDrefExplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, compareValue.Id, flags, imParams))
                                : builder.InsertData(new OpImageSampleDrefImplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, compareValue.Id, flags, imParams));

                            result = new(sample.IdResult!.Value, sample.IdResultType!.Value);
                            accessor.Type = resultType;
                        }
                        else
                            throw new InvalidOperationException("Invalid Sample method call");
                        break;
                    }
                case (PointerType { BaseType: BufferType or TextureType } pointerType, MethodCall { Name.Name: "Load", Parameters.Values.Count: 1 } load):
                    {
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds, i - 1);

                        var resultType = new VectorType(pointerType.BaseType switch
                        {
                            BufferType b => b.BaseType,
                            TextureType t => t.ReturnType,
                        }, 4);

                        var resource = builder.AsValue(context, result);
                        var returnType = context.GetOrRegister(resultType);
                        var coords = load.Parameters.Values[0].CompileAsValue(table, compiler);
                        var loadResult = builder.Insert(new OpImageFetch(returnType, context.Bound++, resource.Id, coords.Id, null, []));
                        result = new(loadResult.ResultId, loadResult.ResultType);
                        accessor.Type = resultType;
                        break;
                    }
                case (PointerType { BaseType: BufferType b }, IndexerExpression indexer):
                {
                    throw new NotImplementedException();
                }
                case (PointerType { BaseType: StructuredBufferType bufferType }, IndexerExpression indexer):
                {
                    // StructuredBuffer are declared as OpTypeStruct { OpTypeRuntimeArray }
                    // so first, we push a 0 to access the OpTypeRuntimeArray
                    PushAccessChainId(accessChainIds, context.CompileConstant(0).Id);
                    // Then we push the index inside the array
                    var indexerValue = indexer.Index.CompileAsValue(table, compiler);
                    PushAccessChainId(accessChainIds, indexerValue.Id);
                    accessor.Type = new PointerType(bufferType.BaseType, Specification.StorageClass.StorageBuffer);
                    break;
                }
                case (PointerType { BaseType: TextureType textureType }, IndexerExpression indexer):
                {
                    var resultType = new VectorType(textureType.ReturnType, 4);

                    var imageValue = builder.AsValue(context, result);
                    var imageCoordValue = ConvertTexCoord(context, builder, textureType, indexer.Index.CompileAsValue(table, compiler), ScalarType.From("int"));
                    var imageRead = builder.Insert(new OpImageRead(context.GetOrRegister(resultType), context.Bound++, imageValue.Id, imageCoordValue.Id, null, []));
                    
                    result = new(imageRead.ResultId, imageRead.ResultType);
                    accessor.Type = resultType;

                    break;
                }
                case (_, MethodCall methodCall):
                    // Emit OpAccessChain with everything so far
                    EmitOpAccessChain(accessChainIds, i - 1);

                    methodCall.MemberCall = result;
                    result = methodCall.Compile(table, compiler);
                    break;
                case (PointerType { BaseType: LoadedShaderSymbol s }, Identifier field):
                    // Emit OpAccessChain with everything so far
                    EmitOpAccessChain(accessChainIds, i - 1);

                    if (!s.TryResolveSymbol(table, context, field.Name, out var matchingComponent))
                        throw new InvalidOperationException();

                    // TODO: figure out instance (this vs composition)
                    result = Identifier.EmitSymbol(builder, context, matchingComponent, false, result.Id);
                    accessor.Type = matchingComponent.Type;
                    break;
                case (PointerType { BaseType: StreamsType s } p, Identifier streamVar):
                    // Since STREAMS struct is built later for each shader, we simply make a reference to variable for now\
                    streamVar.AllowStreamVariables = true;
                    var streamVariableResult = streamVar.Compile(table, compiler);
                    PushAccessChainId(accessChainIds, streamVariableResult.Id);
                    accessor.Type = streamVar.Type;
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
                case (PointerType { BaseType: VectorType v } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    if (swizzle.Length > 1)
                    {
                        // Load value
                        EmitOpAccessChain(accessChainIds, i - 1);
                        result = new(builder.InsertData(new OpLoad(context.GetOrRegister(v), context.Bound++, result.Id, null, [])));

                        Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                        for (int j = 0; j < swizzle.Length; ++j)
                        {
                            swizzleIndices[j] = ConvertSwizzle(swizzle[j]);
                            if (swizzleIndices[j] >= v.Size)
                                throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                        }

                        (result, _) = builder.ApplyVectorSwizzles(context, result, v, swizzleIndices);
                        
                        accessor.Type = new VectorType(v.BaseType, swizzle.Length);
                    }
                    else
                    {
                        PushAccessChainId(accessChainIds, context.CompileConstant(ConvertSwizzle(swizzle[0])).Id);
                        accessor.Type = new PointerType(v.BaseType, p.StorageClass);
                    }
                    break;
                case (VectorType v, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    {
                        Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                        for (int j = 0; j < swizzle.Length; ++j)
                        {
                            swizzleIndices[j] = ConvertSwizzle(swizzle[j]);
                            if (swizzleIndices[j] >= v.Size)
                                throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                        }

                        (result, _) = builder.ApplyVectorSwizzles(context, result, v, swizzleIndices);
                        accessor.Type = v.BaseType.GetVectorOrScalar(swizzle.Length);

                        break;
                    }
                case (PointerType { BaseType: ScalarType s } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    if (swizzle.Length > 1)
                    {
                        // Load value
                        EmitOpAccessChain(accessChainIds, i - 1);
                        result = new(builder.InsertData(new OpLoad(context.GetOrRegister(s), context.Bound++, result.Id, null, [])));

                        Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                        for (int j = 0; j < swizzle.Length; ++j)
                        {
                            swizzleIndices[j] = ConvertSwizzle(swizzle[j]);
                            if (swizzleIndices[j] != 0)
                                throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                        }

                        (result, _) = builder.ApplyScalarSwizzles(context, result, s, swizzleIndices);
                        accessor.Type = new VectorType(s, swizzle.Length);
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
                    throw new NotImplementedException();
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
                // For indexer accessor into non pointer types, we can't use OpCompositeExtract (it expects a constant)
                // So we load the value into a variable and use normal path
                case (ArrayType or VectorType or MatrixType, IndexerExpression indexer):
                    {
                        // We need to load as a variable to use OpAccessChain
                        accessor.Type = new PointerType(currentValueType, Specification.StorageClass.Function);
                        var functionVariable = builder.AddFunctionVariable(context.GetOrRegister(accessor.Type), context.Bound++);
                        builder.Insert(new OpStore(functionVariable, result.Id, null, []));
                        // Process again the same item with new type
                        --i;
                        break;
                    }
                case (PointerType { BaseType: var type }, PostfixIncrement postfix):
                    {
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds, i - 1);

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
                        builder.Insert(new OpStore(resultPointer.Id, modifiedValue.Id, null, []));

                        break;
                    }
                default:
                    throw new NotImplementedException($"unknown accessor {accessor} in expression {this}");
            }

            currentValueType = accessor.Type;
            // only if OpAccessChain is emitted (otherwise there is no value)
            if (accessChainIdCount == 0)
                intermediateValues[1 + i] = result;
        }

        EmitOpAccessChain(accessChainIds, Accessors.Count - 1);

        Type = currentValueType;

        return result;
    }
    
    SpirvValue ConvertTexCoord(SpirvContext context, SpirvBuilder builder, TextureType textureType, SpirvValue spirvValue, ScalarType baseType)
    {
        var textureCoordSize = textureType switch
        {
            Texture1DType => 1,
            Texture2DType => 2,
            Texture3DType or TextureCubeType => 3,
        };
        if (textureType.Arrayed)
            textureCoordSize++;
        spirvValue = builder.Convert(context, spirvValue, baseType.GetVectorOrScalar(textureCoordSize));
        return spirvValue;
    }

    SpirvValue ConvertOffset(SpirvContext context, SpirvBuilder builder, TextureType textureType, SpirvValue spirvValue)
    {
        var offsetSize = textureType switch
        {
            Texture1DType => 1,
            Texture2DType => 2,
            Texture3DType or TextureCubeType => 3,
        };

        spirvValue = builder.Convert(context, spirvValue, ScalarType.From("int").GetVectorOrScalar(offsetSize));
        return spirvValue;
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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var expectedOperandType = Op switch
        {
            Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod => expectedType,
            // TODO: review XOR/OR/Shift etc.
            _ => null,
        };
        
        var left = Left.CompileAsValue(table, compiler, expectedOperandType);
        var right = Right.CompileAsValue(table, compiler, expectedOperandType);

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

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;

        var conditionValue = Condition.CompileAsValue(table, compiler);

        var leftValueBuffer = new NewSpirvBuffer();
        var rightValueBuffer = new NewSpirvBuffer();

        // We store left/right values in temporary buffer: we need to emit them now to know type but we don't want to actually insert them later in builder buffer
        SpirvValue leftResult;
        using (builder.UseTemporaryBuffer(leftValueBuffer))
            leftResult = Left.CompileAsValue(table, compiler);
        SpirvValue rightResult;
        using (builder.UseTemporaryBuffer(rightValueBuffer))
            rightResult = Right.CompileAsValue(table, compiler);

        if (Condition.ValueType.GetElementType() is not ScalarType { TypeName: "bool" })
            table.Errors.Add(new(Condition.Info, SDSLErrorMessages.SDSL0106));

        var scalarType = SpirvBuilder.FindCommonBaseTypeForBinaryOperation(Left.ValueType.GetElementType(), Right.ValueType.GetElementType());
        var resultType = (Condition.ValueType, Left.ValueType, Right.ValueType) switch
        {
            // If condition is a vector, we need to use this vector size instead
            (VectorType c, _, _) => new VectorType(scalarType, c.Size),
            (ScalarType c, ScalarType, ScalarType) => scalarType,
            (ScalarType c, VectorType v1, ScalarType s2) => v1.WithElementType(scalarType),
            (ScalarType c, ScalarType s1, VectorType v2) => v2.WithElementType(scalarType),
            (ScalarType c, VectorType v1, VectorType v2) => new VectorType(scalarType, Math.Min(v1.Size, v2.Size)),
        };

        // Convert type for Left/Right
        using (builder.UseTemporaryBuffer(leftValueBuffer))
            leftResult = builder.Convert(context, leftResult, resultType);
        using (builder.UseTemporaryBuffer(rightValueBuffer))
            rightResult = builder.Convert(context, rightResult, resultType);

        // TODO: Review choice between if/else like branch (OpBranchConditional) which evaluate only one side, or select (OpSelect) which evaluate both side but can work per component but is limited to specific types
        //       It seems HLSL 2021 changed the behavior to align it with C-style short-circuiting.
        // For now, we use OpSelect only with per-component, otherwise we use if/else branching
        bool isBranching = Condition.ValueType is ScalarType;

        if (isBranching)
        {
            var resultVariable = context.Bound++;
            builder.AddFunctionVariable(context.GetOrRegister(new PointerType(resultType, Specification.StorageClass.Function)), resultVariable);

            var blockMergeId = context.Bound++;
            var blockTrueId = context.Bound++;
            var blockFalseId = context.Bound++;

            // OpSelectionMerge and OpBranchConditional (will be filled later)
            builder.Insert(new OpSelectionMerge(blockMergeId, SelectionControlMask.None));
            builder.Insert(new OpBranchConditional(conditionValue.Id, blockTrueId, blockFalseId, []));

            // Block when choosing left value
            builder.CreateBlock(context, blockTrueId, $"ternary_true");
            builder.Merge(leftValueBuffer);
            builder.Insert(new OpStore(resultVariable, leftResult.Id, null, []));
            builder.Insert(new OpBranch(blockMergeId));

            // Block when choosing right value
            builder.CreateBlock(context, blockFalseId, $"ternary_false");
            builder.Merge(rightValueBuffer);
            builder.Insert(new OpStore(resultVariable, rightResult.Id, null, []));
            builder.Insert(new OpBranch(blockMergeId));

            builder.CreateBlock(context, blockMergeId, "ternary_merge");
            var result = builder.Insert(new OpLoad(context.GetOrRegister(resultType), context.Bound++, resultVariable, null, []));
            return new(result.ResultId, result.ResultType);
        }
        else
        {
            if (resultType is VectorType v && Condition.ValueType is ScalarType conditionScalar)
            {
                conditionValue = builder.Convert(context, conditionValue, new VectorType(conditionScalar, v.Size));
            }

            builder.Merge(leftValueBuffer);
            builder.Merge(rightValueBuffer);

            var result = builder.Insert(new OpSelect(context.GetOrRegister(resultType), context.Bound++, conditionValue.Id, leftResult.Id, rightResult.Id));
            return new(result.ResultId, result.ResultType);
        }
    }

    public override string ToString()
    {
        return $"({Condition} ? {Left} : {Right})";
    }
}