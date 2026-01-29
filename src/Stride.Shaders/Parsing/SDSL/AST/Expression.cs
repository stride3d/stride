using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Diagnostics;
using System.Text;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsing.SDSL.AST;



/// <summary>
/// Code expression, represents operations and literals
/// </summary>
public abstract class Expression(TextLocation info) : ValueNode(info)
{
    /// <summary>
    /// Compute <see cref="Type"/> and optionally emit diagnostics.
    /// </summary>
    /// <param name="table"></param>
    /// <param name="expectedType"></param>
    public virtual void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null) => throw new NotImplementedException($"Symbol table cannot process type : {GetType().Name}");
    
    public SpirvValue Compile(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        if (Type == null)
            throw new InvalidOperationException($"{nameof(ProcessSymbol)} was not called on expression {this}");
        
        var result = CompileImpl(table, compiler);
        
        // Check types are matching
        if (result.TypeId != 0 && Type != compiler.Context.ReverseTypes[result.TypeId])
            throw new InvalidOperationException($"{nameof(ProcessSymbol)} computed type {Type} but {nameof(Compile)} created a value of type {compiler.Context.ReverseTypes[result.TypeId]} on expression {this}");

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

    public abstract SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler);

    public SymbolType? ValueType => Type?.GetValueType();

    public virtual SpirvValue CompileAsValue(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var result = Compile(table, compiler, expectedType);
        result = compiler.Builder.AsValue(compiler.Context, result);
        return result;
    }
}

/// <summary>
/// Used only for <see cref="TypeName.ArraySize"/> when size is not explicitly defined.
/// </summary>
/// <param name="info"></param>
public class EmptyExpression(TextLocation info) : Expression(info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null) => throw new NotImplementedException();
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler) => throw new NotImplementedException();
    public override string ToString() => string.Empty;
}

public class MethodCall(Identifier name, ShaderExpressionList arguments, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Arguments = arguments;
    
    public SymbolType? MemberCallBaseType { get; set; }
    public SpirvValue? MemberCall { get; set; }

    public Symbol ResolvedFunctionSymbol { get; set; }
    
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table);
        if (!TryResolveFunctionSymbol(table, out var functionSymbol))
            return;

        var functionType = (FunctionType)functionSymbol.Type;
        Type = functionType.ReturnType;

        ResolvedFunctionSymbol = functionSymbol;
    }

    public void ProcessParameterSymbols(SymbolTable table, FunctionType? functionType = null)
    {
        for (var index = 0; index < Arguments.Values.Count; index++)
        {
            var parameter = Arguments.Values[index];
            var parameterExpectedType = functionType?.ParameterTypes[index].Type;
            parameter.ProcessSymbol(table, parameterExpectedType);
        }
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var functionSymbol = LoadedShaderSymbol.ImportSymbol(table, context, ResolvedFunctionSymbol);
        var functionType = (FunctionType)functionSymbol.Type;

        Type = functionType.ReturnType;

        Span<int> compiledParams = stackalloc int[functionType.ParameterTypes.Count];
        
        if (arguments.Values.Count > functionType.ParameterTypes.Count)
            throw new InvalidOperationException($"Function {Name} was called with {arguments.Values.Count} arguments but only {functionType.ParameterTypes.Count} expected");

        for (int i = 0; i < arguments.Values.Count; i++)
        {
            // Wrap param in proper pointer type (function)
            var paramDefinition = functionType.ParameterTypes[i];
            var paramVariable = context.Bound++;
            builder.AddFunctionVariable(context.GetOrRegister(paramDefinition.Type), paramVariable);

            // Note: "in" is implicit, so we match in all cases except if out
            var inOutFlags = paramDefinition.Modifiers & ParameterModifiers.InOut;
            if (inOutFlags != ParameterModifiers.Out)
            {
                var paramSource = arguments.Values[i].CompileAsValue(table, compiler, paramDefinition.Type.GetValueType());

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
        var missingParameters = functionType.ParameterTypes.Count - arguments.Values.Count;
        var defaultParameters = 0;
        if (missingParameters > 0 && functionSymbol.MethodDefaultParameters is {} methodDefaultParameters)
        {
            // Is there enough parameters now?
            if (missingParameters <= methodDefaultParameters.DefaultValues.Length)
            {
                // Import missing parameters
                for (int i = 0; i < missingParameters; ++i)
                {
                    var paramDefinition = functionType.ParameterTypes[arguments.Values.Count + i];

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

                    compiledParams[arguments.Values.Count + i] = paramVariable;
                }
                missingParameters = 0;
            }
        }
        
        if (missingParameters > 0)
            throw new InvalidOperationException($"Function {Name} was called with {arguments.Values.Count} arguments but there was {(defaultParameters > 0 ? $"between {functionType.ParameterTypes.Count - defaultParameters} and {functionType.ParameterTypes.Count}" : functionType.ParameterTypes.Count)} expected");

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
            // Note: we make a copy to not mutate original
            functionSymbol = functionSymbol with { IdRef = builder.Insert(new OpMemberAccessSDSL(context.GetOrRegister(functionType), context.Bound++, instanceId, functionSymbol.IdRef)).ResultId };
        
        var result = builder.CallFunction(table, context, functionSymbol, [.. compiledParams]);

        for (int i = 0; i < arguments.Values.Count; i++)
        {
            var paramDefinition = functionType.ParameterTypes[i];
            if (paramDefinition.Modifiers.HasFlag(ParameterModifiers.Out))
            {
                var paramDefinitionType = (PointerType)paramDefinition.Type;
                var paramVariable = compiledParams[i];
                var paramTarget = arguments.Values[i].Compile(table, compiler, paramDefinitionType);
                var paramTargetType = (PointerType)context.ReverseTypes[paramTarget.TypeId];

                if (paramTargetType.BaseType != paramDefinitionType.BaseType)
                    throw new InvalidOperationException($"Value of type {paramTargetType.BaseType} can't be used as out parameter {i} of type {paramDefinitionType.BaseType} in method call [{this}]");

                var loadedResult = builder.Insert(new OpLoad(context.GetOrRegister(paramTargetType.BaseType), context.Bound++, paramVariable, null, [])).ResultId;
                builder.Insert(new OpStore(paramTarget.Id, loadedResult, null, []));
            }
        }

        return result;
    }

    private bool TryResolveFunctionSymbol(SymbolTable table, out Symbol functionSymbol)
    {
        // Note: for now, TypeId 0 is used for this/base; let's improve that later
        if (MemberCallBaseType is LoadedShaderSymbol loadedShaderSymbol)
        {
            if (!loadedShaderSymbol.TryResolveSymbol(Name, out functionSymbol))
            {
                functionSymbol = default;
                table.AddError(new(info, string.Format(SDSLErrorMessages.SDSL0109, Name)));
                return false;
            }
        }
        else
        {
            functionSymbol = table.ResolveSymbol(Name);
        }

        // Choose appropriate method to call
        // We just care about overload (different signature), not override (base/this/most derived) as this will be resolved in ShaderMixer later
        if (functionSymbol.Type is FunctionGroupType)
        {
            // Note: int.MaxValue means incompatible
            static int OverloadScore(Symbol functionSymbol, ShaderExpressionList arguments)
            {
                // Check argument count
                var functionType = (FunctionType)functionSymbol.Type;
                if (arguments.Values.Count > functionType.ParameterTypes.Count || arguments.Values.Count < functionType.ParameterTypes.Count + (functionSymbol.MethodDefaultParameters?.DefaultValues.Length ?? 0))
                    return int.MaxValue;
                
                // Check if argument can be converted
                var score = 0;
                for (var index = 0; index < arguments.Values.Count; index++)
                {
                    var argument = arguments.Values[index];
                    var parameter =  functionType.ParameterTypes[index];
                    var argScore = SpirvBuilder.CanConvertScore(argument.ValueType, parameter.Type.GetValueType());
                    if (argScore == int.MaxValue)
                        return int.MaxValue;

                    score += argScore;
                }
                
                // method with fewer optional parameters that need to be filled in by default values is generally preferred
                score += functionType.ParameterTypes.Count - arguments.Values.Count;

                return score;
            }
            
            var accessibleMethods = functionSymbol.GroupMembers
                // Check overload score
                .Select(x => (Score: OverloadScore(x, arguments), Symbol: x))
                // Remove non-applicable methods
                .Where(x => x.Score != int.MaxValue)
                // Group by signature/score (we assume method with exact same signature means they are overriding each other, but we might need to do a better check using override info)
                .GroupBy(x => (x.Score, x.Symbol.Type))
                // Sort by best match (score)
                .OrderBy(x => x.Key.Score)
                .ToList();

            if (accessibleMethods.Count == 0)
            {
                table.AddError(new(info, $"Can't find a valid method overload to call for {Name} (among {functionSymbol.GroupMembers} candidate(s))"));
                return false;
            }

            // Check if there is an ambiguous call (multiple method groups with the lowest score)
            if (accessibleMethods.Count > 1 && accessibleMethods[0].Key.Score == accessibleMethods[1].Key.Score)
            {
                table.AddError(new(info, $"Ambiguous method overload when calling for {Name} (among {functionSymbol.GroupMembers} candidate(s))"));
                return false;
            }

            // Note: actual override (base/this) will be reevaluted during ShaderMixer, but overload (different signature) won't be changed
            // So we just pick the method with the lowest score
            functionSymbol = accessibleMethods[0].First().Symbol;
        }

        return true;
    }

    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Arguments)})";
    }
}

/// <summary>
/// Represents an accessed mixin.
/// </summary>
public class MixinAccess(Mixin mixin, TextLocation info) : Expression(info)
{
    public Mixin Mixin { get; set; } = mixin;

    public Symbol ResolvedSymbol { get; set; }

    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        var context = table.Context;
        
        // MixinAccess is same as Identifier static variable case, except we have generics (which is why MixinAccess was chosen over Identifier)
        var generics = SDFX.AST.ShaderEffect.CompileGenerics(table, context, Mixin.Generics);
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

        ResolvedSymbol = symbol;
        Type = symbol.Type;
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        
        return Identifier.EmitSymbol(builder, context, ResolvedSymbol, builder.CurrentFunction == null);
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
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        switch (Operator)
        {
            case Operator.Inc:
            case Operator.Dec:
            case Operator.Not:
            case Operator.Plus:
            case Operator.Minus:
                expression.ProcessSymbol(table, expectedType);
                Type = expression.Type;
                break;
            default:
                table.AddError(new(info, string.Format(SDSLErrorMessages.SDSL0111, $"Prefix operator {Operator}")));
                break;
        }
    }

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
                        if (!isPointer)
                            throw new InvalidOperationException($"Can't use increment/decrement expression on non-pointer expression {Expression}");

                        // Use integer so that it gets converted to proper type according to expression type
                        var constant1 = context.CompileConstant(1);
                        var result = builder.BinaryOperation(table, context, valueExpression, Operator switch
                        {
                            Operator.Inc => Operator.Plus,
                            Operator.Dec => Operator.Minus,
                        }, constant1, info);

                        // We store the modified value back in the variable
                        builder.Insert(new OpStore(expression.Id, result.Id, null, []));

                        Type = type;
                        return expression;
                    }
                case Operator.Not:
                    {
                        if (valueType.GetElementType() is not ScalarType { Type: Scalar.Boolean })
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

    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        TypeName.ProcessSymbol(table);
        Expression.ProcessSymbol(table, expectedType);
        Type = TypeName.Type;
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        TypeName.ProcessSymbol(table);
        var castType = TypeName.Type;
        var value = Expression.CompileAsValue(table, compiler);

        Type = castType;

        return builder.Convert(context, value, castType);
    }
}


public class IndexerExpression(Expression index, TextLocation info) : Expression(info)
{
    public Expression Index { get; set; } = index;

    // Used only in AccessChainExpression
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null) => throw new NotImplementedException();
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler) => throw new NotImplementedException();
    public override string ToString()
    {
        return $"[{Index}]";
    }
}

public class PostfixIncrement(Operator op, TextLocation info) : Expression(info)
{
    public Operator Operator { get; set; } = op;

    // Used only in AccessChainExpression
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null) => throw new NotImplementedException();
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler) => throw new NotImplementedException();
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
        CompileHelper(table, compiler);
        
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
                {
                    var resultType = new VectorType(bufferType.BaseType, 4);
                    var buffer = builder.AsValue(context, lvalueBase);
                    
                    var location = indexer.Index.CompileAsValue(table, compiler);
                    location = builder.Convert(context, location, ScalarType.Int);
                    var bufferValue = builder.Convert(context, rvalue, resultType);
                    builder.Insert(new OpImageWrite(buffer.Id, location.Id, bufferValue.Id, null, []));
                    // We stop there
                    return;
                }
                // ImageWrite
                case (PointerType { BaseType: TextureType textureType }, IndexerExpression indexer):
                {
                    var resultType = new VectorType(textureType.ReturnType, 4);
                    var image = builder.AsValue(context, lvalueBase);
                    
                    var imageCoordValue = ConvertTexCoord(context, builder, textureType, indexer.Index.CompileAsValue(table, compiler), ScalarType.Int);
                    var texelValue = builder.Convert(context, rvalue, resultType);
                    builder.Insert(new OpImageWrite(image.Id, imageCoordValue.Id, texelValue.Id, null, []));
                    // We stop there
                    return;
                }
                case (PointerType { BaseType: MatrixType } or MatrixType, Identifier { Name: var swizzle } id) when id.IsMatrixSwizzle((MatrixType)currentValueType.GetValueType(), out var swizzles):
                    throw new NotImplementedException("Assign back to matrix swizzle is not implemented yet");
                case (PointerType { BaseType: VectorType or ScalarType } or VectorType or ScalarType, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    // Swizzle: we transform the value to assign accordingly
                    
                    // We load the original value (if pointer)
                    var lvalueType = currentValueType;
                    if (lvalueType is PointerType p)
                    {
                        lvalueBase = new(builder.InsertData(new OpLoad(context.GetOrRegister(p.BaseType), context.Bound++, lvalueBase.Id, null, [])));
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

    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        Source.ProcessSymbol(table);
        var currentValueType = Source.Type;

        CompileHelper(table, null);
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        return CompileHelper(table, compiler);
    }

    // Since there are many switch case, we do both ProcessSymbols and Compile in the same functions to make sure to not miss anything (depending on if compiler is set)
    public SpirvValue CompileHelper(SymbolTable table, CompilerUnit? compiler = null)
    {
        if (compiler != null)
        {
            if (intermediateValues != null)
                return intermediateValues[^1];
            intermediateValues = new SpirvValue[Accessors.Count + 1];
        }

        var context = compiler?.Context;
        var builder = compiler?.Builder;
        SpirvValue result = default;

        if (builder != null)
        {
            result = Source.Compile(table, compiler!);
            intermediateValues[0] = result;
        }
        else
        {
            Source.ProcessSymbol(table);
        }
        var currentValueType = Source.Type;

        int accessChainIdCount = 0;
        void PushAccessChainId(Span<int> accessChainIds, int accessChainIndex)
        {
            if (compiler == null)
                throw new InvalidOperationException();
            accessChainIds[accessChainIdCount++] = accessChainIndex;
        }
        
        void EmitOpAccessChain(Span<int> accessChainIds, int? intermediateValueIndex)
        {
            if (compiler == null)
                throw new InvalidOperationException();
            // Do we need to issue an OpAccessChain?
            if (accessChainIdCount > 0)
            {
                var resultType = context.GetOrRegister(currentValueType);
                var accessChain = builder.Insert(new OpAccessChain(resultType, context.Bound++, result.Id, [.. accessChainIds.Slice(0, accessChainIdCount)]));
                result = new SpirvValue(accessChain.ResultId, resultType);
                
                if (intermediateValueIndex != null)
                    intermediateValues[1 + intermediateValueIndex.Value] = result;
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

        VectorType ComputeBufferOrTextureAccessReturnType(PointerType pointerType)
        {
            return pointerType.BaseType switch
            {
                BufferType b => new VectorType(b.BaseType, 4),
                TextureType t => new VectorType(t.ReturnType, 4),
            };
        }

        (SpirvValue Value, SymbolType ResultType) BufferLoad(BufferType bufferType, SpirvValue buffer, Expression locationExpression)
        {
            var resultType = new VectorType(bufferType.BaseType, 4);
            
            buffer = builder.AsValue(context, buffer);
            var location = locationExpression.CompileAsValue(table, compiler);
            location = builder.Convert(context, location, ScalarType.Int);
            
            var loadResult = builder.Insert(new OpImageRead(context.GetOrRegister(resultType), context.Bound++, buffer.Id, location.Id, null, []));
            return (new(loadResult.ResultId, loadResult.ResultType), resultType);
        }
        
        (SpirvValue Value, SymbolType ResultType) TextureLoad(TextureType textureType, SpirvValue buffer, Expression coordinatesExpression, Expression? offsetExpression, Expression? sampleIndexExpression, bool containsLod)
        {
            var resultType = new VectorType(textureType.ReturnType, 4);
            var imageCoordValue = ConvertTexCoord(context, builder, textureType, coordinatesExpression.CompileAsValue(table, compiler), ScalarType.Int, containsLod);
            var imageCoordType = context.ReverseTypes[imageCoordValue.TypeId];
            SpirvValue lod;
            
            if (containsLod)
            {
                // We get all components except last one (LOD)
                var imageCoordSize = imageCoordType.GetElementCount();
                imageCoordType = imageCoordType.GetElementType().GetVectorOrScalar(imageCoordSize - 1);
                Span<int> shuffleIndices = stackalloc int[imageCoordSize - 1];
                for (int i = 0; i < shuffleIndices.Length; ++i)
                    shuffleIndices[i] = i;

                // Note: assign LOD first because we truncate imageCoordValue right after
                // Extract LOD (last coordinate) as a separate value
                lod = new(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(ScalarType.Int), context.Bound++, imageCoordValue.Id, [imageCoordSize - 1])));
                // Remove last component (LOD) from texcoord 
                imageCoordValue = new(builder.InsertData(new OpVectorShuffle(context.GetOrRegister(imageCoordType), context.Bound++, imageCoordValue.Id, imageCoordValue.Id, new(shuffleIndices))));
            }
            else
            {
                lod = context.CompileConstant(0.0f);
            }

            buffer = builder.AsValue(context, buffer);

            SpirvValue? offset = offsetExpression != null
                ? ConvertOffset(context, builder, textureType, offsetExpression.CompileAsValue(table, compiler))
                : null;
            SpirvValue? sampleIndex = sampleIndexExpression != null
                ? builder.Convert(context, sampleIndexExpression.CompileAsValue(table, compiler), ScalarType.Int)
                : null;
            TextureGenerateImageOperands(lod, offset, sampleIndex, out var imask, out var imParams);
            var loadResult = builder.Insert(new OpImageFetch(context.GetOrRegister(resultType), context.Bound++, buffer.Id, imageCoordValue.Id, imask, imParams));
            return (new(loadResult.ResultId, loadResult.ResultType), resultType);
        }

        for (var i = 0; i < Accessors.Count; ++i)
        {
            var accessor = Accessors[i];

            if (compiler == null)
                CoalesceSwizzles(i, currentValueType, ref accessor);

            switch (currentValueType, accessor)
            {
                case (PointerType { BaseType: TextureType textureType },
                        MethodCall { Name.Name: "Sample", Arguments.Values.Count: 2 or 3 }
                        or MethodCall { Name.Name: "SampleLevel", Arguments.Values.Count: 3 or 4 }):
                    {
                        if (compiler == null)
                        {
                            ((MethodCall)accessor).ProcessParameterSymbols(table, null);
                            accessor.Type = new VectorType(textureType.ReturnType, 4);
                            break;
                        }
                        
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds, i - 1);
                        var textureValue = builder.AsValue(context, result);
                        var resultType = accessor.Type;

                        if (accessor is MethodCall { Name.Name: "Sample", Arguments.Values.Count: 2 or 3 } implicitSampling)
                        {
                            var samplerValue = implicitSampling.Arguments.Values[0].CompileAsValue(table, compiler);
                            var texCoordValue = ConvertTexCoord(context, builder, textureType, implicitSampling.Arguments.Values[1].CompileAsValue(table, compiler), ScalarType.Float);

                            var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
                            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                            var returnType = context.GetOrRegister(resultType);

                            SpirvValue? offset = implicitSampling.Arguments.Values.Count >= 3
                                ? ConvertOffset(context, builder, textureType, implicitSampling.Arguments.Values[2].CompileAsValue(table, compiler))
                                : null;
                            TextureGenerateImageOperands(null, offset, null, out var imask, out var imParams);
                            var sample = builder.Insert(new OpImageSampleImplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, imask, imParams));

                            result = new(sample.ResultId, sample.ResultType);
                            accessor.Type = resultType;
                        }
                        else if (accessor is MethodCall { Name.Name: "SampleLevel", Arguments.Values.Count: 3 or 4 } explicitSampling)
                        {
                            var samplerValue = explicitSampling.Arguments.Values[0].CompileAsValue(table, compiler);
                            var texCoordValue = ConvertTexCoord(context, builder, textureType, explicitSampling.Arguments.Values[1].CompileAsValue(table, compiler), ScalarType.Float);
                            
                            var levelValue = explicitSampling.Arguments.Values[2].CompileAsValue(table, compiler);
                            levelValue = builder.Convert(context, levelValue, ScalarType.Float);

                            var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
                            var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                            var returnType = context.GetOrRegister(resultType);

                            SpirvValue? offset = explicitSampling.Arguments.Values.Count >= 4
                                ? ConvertOffset(context, builder, textureType, explicitSampling.Arguments.Values[3].CompileAsValue(table, compiler))
                                : null;
                            TextureGenerateImageOperands(levelValue, offset, null, out var imask, out var imParams);
                            var sample = builder.Insert(new OpImageSampleExplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, imask, imParams));

                            result = new(sample.ResultId, sample.ResultType);
                            accessor.Type = resultType;
                        }
                        else
                            throw new InvalidOperationException("Invalid Sample method call");
                        break;
                    }
                case (PointerType { BaseType: TextureType textureType },
                    MethodCall { Name.Name: "SampleCmp" or "SampleCmpLevelZero", Arguments.Values.Count: 3 or 4 } sampleCompare):
                {
                    if (compiler == null)
                    {
                        ((MethodCall)accessor).ProcessParameterSymbols(table, null);
                        accessor.Type = textureType.ReturnType;
                        if (accessor.Type is not ScalarType)
                            throw new InvalidOperationException();
                        break;
                    }

                    var resultType = textureType.ReturnType;

                    // Emit OpAccessChain with everything so far
                    EmitOpAccessChain(accessChainIds, i - 1);
                    var textureValue = builder.AsValue(context, result);
                    
                    var samplerValue = sampleCompare.Arguments.Values[0].CompileAsValue(table, compiler);
                    var texCoordValue = ConvertTexCoord(context, builder, textureType, sampleCompare.Arguments.Values[1].CompileAsValue(table, compiler), ScalarType.Float);
                    
                    var compareValue = sampleCompare.Arguments.Values[2].CompileAsValue(table, compiler);

                    var typeSampledImage = context.GetOrRegister(new SampledImage(textureType));
                    var sampledImage = builder.Insert(new OpSampledImage(typeSampledImage, context.Bound++, textureValue.Id, samplerValue.Id));
                    var returnType = context.GetOrRegister(resultType);
                    
                    SpirvValue? offset = sampleCompare.Arguments.Values.Count >= 4
                        ? ConvertOffset(context, builder, textureType, sampleCompare.Arguments.Values[3].CompileAsValue(table, compiler))
                        : null;
                    TextureGenerateImageOperands(context.CompileConstant(0.0f), offset, null, out var imask, out var imParams);
                    var sample = sampleCompare.Name.Name == "SampleCmpLevelZero"
                        ? builder.InsertData(new OpImageSampleDrefExplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, compareValue.Id, imask, imParams))
                        : builder.InsertData(new OpImageSampleDrefImplicitLod(returnType, context.Bound++, sampledImage.ResultId, texCoordValue.Id, compareValue.Id, imask, imParams));

                    result = new(sample.IdResult!.Value, sample.IdResultType!.Value);
                    accessor.Type = resultType;
                    break;
                }
                case (PointerType { BaseType: BufferType or TextureType } pointerType, MethodCall { Name.Name: "Load", Arguments.Values.Count: 1 or 2 or 3 } load):
                    {
                        if (compiler == null)
                        {
                            // Check parameter count
                            switch (pointerType.BaseType)
                            {
                                case BufferType b:
                                    if (load.Arguments.Values.Count != 1)
                                        table.AddError(new(info, "Buffer.Load expects a single argument"));
                                    break;
                                case TextureType t:
                                    var requiredArguments = 1;
                                    if (t.Multisampled)
                                        requiredArguments++;
                                    
                                    // One optional argument (offset)
                                    if (load.Arguments.Values.Count != requiredArguments && load.Arguments.Values.Count != requiredArguments + 1)
                                        table.AddError(new(info, $"Texture.Load expects {requiredArguments} or {requiredArguments + 1} arguments"));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(pointerType.BaseType));
                            }
                            
                            ((MethodCall)accessor).ProcessParameterSymbols(table, null);
                            accessor.Type = ComputeBufferOrTextureAccessReturnType(pointerType);
                            break;
                        }

                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds, i - 1);

                        switch (pointerType.BaseType)
                        {
                            case BufferType b:
                                (result, accessor.Type) = BufferLoad(b, result, load.Arguments.Values[0]);
                                break;
                            case TextureType t:
                                var sampleIndex = t.Multisampled ? load.Arguments.Values[1] : null;
                                var offsetArgIndex = t.Multisampled ? 2 : 1;
                                var offset = load.Arguments.Values.Count >= offsetArgIndex + 1 ? load.Arguments.Values[offsetArgIndex] : null;
                                (result, accessor.Type) = TextureLoad(t, result, load.Arguments.Values[0], offset, sampleIndex, true);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(pointerType.BaseType));
                        }

                        break;
                    }
                case (PointerType { BaseType: BufferType or TextureType } pointerType, IndexerExpression indexer):
                {
                    if (compiler == null)
                    {
                        indexer.Index.ProcessSymbol(table);
                        accessor.Type = ComputeBufferOrTextureAccessReturnType(pointerType);
                        break;
                    }

                    // Emit OpAccessChain with everything so far
                    EmitOpAccessChain(accessChainIds, i - 1);

                    (result, accessor.Type) = pointerType.BaseType switch
                    {
                        BufferType b => BufferLoad(b, result, indexer.Index),
                        TextureType t => TextureLoad(t, result, indexer.Index, null, null, false),
                    };
                    break;
                }
                case (PointerType { BaseType: StructuredBufferType bufferType }, IndexerExpression indexer):
                {
                    if (compiler == null)
                    {
                        indexer.Index.ProcessSymbol(table);
                        accessor.Type = new PointerType(bufferType.BaseType, Specification.StorageClass.StorageBuffer);
                        break;
                    }

                    // StructuredBuffer are declared as OpTypeStruct { OpTypeRuntimeArray }
                    // so first, we push a 0 to access the OpTypeRuntimeArray
                    PushAccessChainId(accessChainIds, context.CompileConstant(0).Id);
                    // Then we push the index inside the array
                    var indexerValue = indexer.Index.CompileAsValue(table, compiler);
                    PushAccessChainId(accessChainIds, indexerValue.Id);
                    break;
                }
                case (_, MethodCall methodCall):
                    if (compiler == null)
                    {
                        methodCall.MemberCallBaseType = ((PointerType)currentValueType).BaseType;
                        methodCall.ProcessSymbol(table);
                        break;
                    }
                    
                    // Emit OpAccessChain with everything so far
                    EmitOpAccessChain(accessChainIds, i - 1);
                    methodCall.MemberCall = result;
                    result = methodCall.Compile(table, compiler);
                    break;
                case (PointerType { BaseType: LoadedShaderSymbol s }, Identifier field):
                    if (compiler == null)
                    {
                        if (!s.TryResolveSymbol(field.Name, out var matchingComponent))
                        {
                            table.AddError(new(info, string.Format(SDSLErrorMessages.SDSL0112, field.Name, new AccessorChainExpression(Source, info) { Accessors = Accessors[0..i] }, currentValueType)));
                            return default;
                        }

                        field.ResolvedSymbol = matchingComponent;
                        accessor.Type = matchingComponent.Type;
                        break;
                    }

                    var importedVariable = LoadedShaderSymbol.ImportSymbol(table, context, field.ResolvedSymbol);
                    
                    // Emit OpAccessChain with everything so far
                    EmitOpAccessChain(accessChainIds, i - 1);
                    
                    // TODO: figure out instance (this vs composition)
                    result = Identifier.EmitSymbol(builder, context, importedVariable, false, result.Id);
                    break;
                case (PointerType { BaseType: StreamsType s } p, Identifier streamVar):
                    if (compiler == null)
                    {
                        streamVar.AllowStreamVariables = true;
                        streamVar.ProcessSymbol(table);
                        accessor.Type = streamVar.Type;
                        break;
                    }

                    // Since STREAMS struct is built later for each shader, we simply make a reference to variable for now\
                    var streamVariableResult = streamVar.Compile(table, compiler);
                    PushAccessChainId(accessChainIds, streamVariableResult.Id);
                    break;
                case (PointerType { BaseType: StructType s } p, Identifier field):
                    var index = s.TryGetFieldIndex(field);
                    if (compiler == null)
                    {
                        if (index == -1)
                        {
                            table.AddError(new(info, string.Format(SDSLErrorMessages.SDSL0113, field.Name, currentValueType)));
                            return default;
                        }

                        accessor.Type = new PointerType(s.Members[index].Type, p.StorageClass);
                        break;
                    }
                    //indexes[i] = builder.CreateConstant(context, shader, new IntegerLiteral(new(32, false, true), index, new())).Id;
                    PushAccessChainId(accessChainIds, context.CompileConstant(index).Id);
                    break;
                // Swizzles
                case (PointerType { BaseType: MatrixType m } p, Identifier id) when id.IsMatrixSwizzle(m, out var swizzles):
                {
                    if (swizzles.Count > 1)
                    {
                        if (compiler == null)
                        {
                            if (swizzles.Count > 4)
                            {
                                table.AddError(new(info, $"more than four positions are referenced in matrix to vector swizzle"));
                                return default;
                            }

                            accessor.Type = new VectorType(m.BaseType, swizzles.Count);
                            break;
                        }

                        EmitOpAccessChain(accessChainIds, i - 1);
                        (result, accessor.Type) = builder.ApplyMatrixSwizzles(context, result, m, swizzles.AsSpan());
                    }
                    else
                    {
                        // Keep as a pointer
                        if (compiler == null)
                        {
                            accessor.Type = new PointerType(m.BaseType, p.StorageClass);
                            break;
                        }

                        PushAccessChainId(accessChainIds, context.CompileConstant(swizzles[0].Column).Id);
                        PushAccessChainId(accessChainIds, context.CompileConstant(swizzles[0].Row).Id);
                    }
                    break;
                }
                case (MatrixType m, Identifier id) when id.IsMatrixSwizzle(m, out var swizzles):
                {
                    if (compiler == null)
                    {
                        if (swizzles.Count > 4)
                        {
                            table.AddError(new(info, $"more than four positions are referenced in matrix to vector swizzle"));
                            return default;
                        }

                        accessor.Type = m.BaseType.GetVectorOrScalar(swizzles.Count);
                        break;
                    }

                    (result, accessor.Type) = builder.ApplyMatrixSwizzles(context, result, m, swizzles.AsSpan());
                    break;
                }
                case (PointerType { BaseType: VectorType v } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    if (swizzle.Length > 1)
                    {
                        if (compiler == null)
                        {
                            accessor.Type = new VectorType(v.BaseType, swizzle.Length);
                            break;
                        }

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
                    }
                    else
                    {
                        // Keep as a pointer
                        if (compiler == null)
                        {
                            accessor.Type = new PointerType(v.BaseType, p.StorageClass);
                            break;
                        }

                        PushAccessChainId(accessChainIds, context.CompileConstant(ConvertSwizzle(swizzle[0])).Id);
                    }
                    break;
                case (VectorType v, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    {
                        if (compiler == null)
                        {
                            accessor.Type = v.BaseType.GetVectorOrScalar(swizzle.Length);
                            break;
                        }

                        Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                        for (int j = 0; j < swizzle.Length; ++j)
                        {
                            swizzleIndices[j] = ConvertSwizzle(swizzle[j]);
                            if (swizzleIndices[j] >= v.Size)
                                throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                        }

                        (result, _) = builder.ApplyVectorSwizzles(context, result, v, swizzleIndices);

                        break;
                    }
                case (PointerType { BaseType: ScalarType s } p, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    if (swizzle.Length > 1)
                    {
                        if (compiler == null)
                        {
                            accessor.Type = new VectorType(s, swizzle.Length);
                            break;
                        }

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
                    }
                    else
                    {
                        // Do nothing
                        if (compiler == null)
                        {
                            accessor.Type = currentValueType;
                            break;
                        }

                        if (ConvertSwizzle(swizzle[0]) != 0)
                            throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                    }
                    break;
                case (ScalarType s, Identifier { Name: var swizzle } id) when id.IsVectorSwizzle():
                    if (swizzle.Length > 1)
                    {
                        if (compiler == null)
                        {
                            accessor.Type = s.GetVectorOrScalar(swizzle.Length);
                            break;
                        }

                        Span<int> swizzleIndices = stackalloc int[swizzle.Length];
                        for (int j = 0; j < swizzle.Length; ++j)
                        {
                            swizzleIndices[j] = ConvertSwizzle(swizzle[j]);
                            if (swizzleIndices[j] != 0)
                                throw new InvalidOperationException($"Swizzle {accessor} is out of bound for expression {ToString(i)} of type {currentValueType}");
                        }

                        (result, _) = builder.ApplyScalarSwizzles(context, result, s, swizzleIndices);
                    }
                    else
                    {
                        // Do nothing
                        if (compiler == null)
                        {
                            accessor.Type = currentValueType;
                            break;
                        }
                    }
                    break;
                // Array indexer for shader compositions
                case (PointerType { BaseType: ArrayType { BaseType: ShaderSymbol s } }, IndexerExpression { Index: IntegerLiteral { Value: var compositionIndex } }):
                    throw new NotImplementedException();
                // Array indexer for arrays
                case (PointerType { BaseType: ArrayType { BaseType: var t } } p, IndexerExpression indexer):
                    {
                        if (compiler == null)
                        {
                            indexer.Index.ProcessSymbol(table);
                            accessor.Type = new PointerType(t, p.StorageClass);
                            break;
                        }
                        var indexerValue = indexer.Index.CompileAsValue(table, compiler);
                        PushAccessChainId(accessChainIds, indexerValue.Id);
                        break;
                    }
                // Array indexer for vector/matrix
                case (PointerType { BaseType: VectorType or MatrixType } p, IndexerExpression indexer):
                    {
                        if (compiler == null)
                        {
                            indexer.Index.ProcessSymbol(table);
                            accessor.Type = new PointerType(p.BaseType switch
                            {
                                MatrixType m => new VectorType(m.BaseType, m.Rows),
                                VectorType v => v.BaseType,
                            }, p.StorageClass);
                            break;
                        }

                        var indexerValue = indexer.Index.CompileAsValue(table, compiler);
                        PushAccessChainId(accessChainIds, indexerValue.Id);
                        break;
                    }
                // For indexer accessor into non pointer types, we can't use OpCompositeExtract (it expects a constant)
                // So we load the value into a variable and use normal path
                case (ArrayType or VectorType or MatrixType, IndexerExpression indexer):
                    {
                        if (compiler == null)
                        {
                            accessor.Type = new PointerType(currentValueType, Specification.StorageClass.Function);
                            break;
                        }

                        // We need to load as a variable to use OpAccessChain
                        var functionVariable = builder.AddFunctionVariable(context.GetOrRegister(accessor.Type), context.Bound++);
                        builder.Insert(new OpStore(functionVariable, result.Id, null, []));
                        // Process again the same item with new type
                        --i;
                        break;
                    }
                case (PointerType { BaseType: var type }, PostfixIncrement postfix):
                    {
                        if (compiler == null)
                        {
                            accessor.Type = type;
                            break;
                        }
                        
                        // Emit OpAccessChain with everything so far
                        EmitOpAccessChain(accessChainIds, i - 1);

                        var resultPointer = result;

                        // This is what this chain return (value before modification)
                        result = builder.AsValue(context, result);

                        // Use integer so that it gets converted to proper type according to expression type
                        var constant1 = context.CompileConstant(1);
                        var modifiedValue = builder.BinaryOperation(table, context, result, postfix.Operator switch
                        {
                            Operator.Inc => Operator.Plus,
                            Operator.Dec => Operator.Minus,
                        }, constant1, info);

                        // We store the modified value back in the variable
                        builder.Insert(new OpStore(resultPointer.Id, modifiedValue.Id, null, []));

                        break;
                    }
                default:
                    throw new NotImplementedException($"unknown accessor {accessor} in expression {this}");
            }

            currentValueType = accessor.Type;
            // only if OpAccessChain is emitted (otherwise there is no value)
            if (compiler != null && accessChainIdCount == 0)
                intermediateValues[1 + i] = result;
        }

        if (compiler != null)
            EmitOpAccessChain(accessChainIds, Accessors.Count - 1);

        Type = currentValueType;

        return result;
    }

    private void TextureGenerateImageOperands(SpirvValue? lod, SpirvValue? offset, SpirvValue? sampleIndex, out ImageOperandsMask imask, out EnumerantParameters imParams)
    {
        imask = ImageOperandsMask.None;
        // Allocate for worst case (3 operands)
        Span<int> operands = stackalloc int[3];
        int operandCount = 0;
        if (lod != null)
        {
            imask |= ImageOperandsMask.Lod;
            operands[operandCount++] = lod.Value.Id;
        }
        if (offset != null)
        {
            imask |= ImageOperandsMask.Offset;
            operands[operandCount++] = offset.Value.Id;
        }
        if (sampleIndex != null)
        {
            imask |= ImageOperandsMask.Sample;
            operands[operandCount++] = sampleIndex.Value.Id;
        }

        imParams = operandCount > 0 ? new EnumerantParameters(operands.Slice(0, operandCount)) : new EnumerantParameters();
    }

    SpirvValue ConvertTexCoord(SpirvContext context, SpirvBuilder builder, TextureType textureType, SpirvValue spirvValue, ScalarType baseType, bool hasLod = false)
    {
        var textureCoordSize = textureType switch
        {
            Texture1DType => 1,
            Texture2DType => 2,
            Texture3DType or TextureCubeType => 3,
        };
        if (textureType.Arrayed)
            textureCoordSize++;
        if (hasLod)
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

        spirvValue = builder.Convert(context, spirvValue, ScalarType.Int.GetVectorOrScalar(offsetSize));
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

    private SymbolType expectedOperandType;

    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        var expectedOperandType = Op switch
        {
            Operator.Plus or Operator.Minus or Operator.Mul or Operator.Div or Operator.Mod => expectedType,
            // TODO: review XOR/OR/Shift etc.
            _ => null,
        };
        
        Left.ProcessSymbol(table, expectedOperandType);
        Right.ProcessSymbol(table, expectedOperandType);

        var analysisResult = SpirvBuilder.AnalyzeBinaryOperation(table, Left.ValueType, Op, Right.ValueType, info);
        
        // If type is different than expected, try again with proper type
        // this will help in some cases (i.e. emit literal as float instead of integer, which is necessary for constants)
        expectedOperandType = analysisResult?.OperandType;
        if (Left.ValueType != expectedOperandType)
            Left.ProcessSymbol(table, expectedOperandType);
        if (Right.ValueType != expectedOperandType)
            Right.ProcessSymbol(table, expectedOperandType);

        Type = analysisResult?.ResultType;
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var left = Left.CompileAsValue(table, compiler, expectedOperandType);
        var right = Right.CompileAsValue(table, compiler, expectedOperandType);

        var (builder, context) = compiler;
        var result = builder.BinaryOperation(table, context, left, Op, right, info);
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

    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        Condition.ProcessSymbol(table);
        
        if (Condition.ValueType.GetElementType() is not ScalarType { Type: Scalar.Boolean })
            table.AddError(new(Condition.Info, SDSLErrorMessages.SDSL0106));
        
        Left.ProcessSymbol(table);
        Right.ProcessSymbol(table);

        var scalarType = SpirvBuilder.FindCommonBaseTypeForBinaryOperation(Left.ValueType.GetElementType(), Right.ValueType.GetElementType());
        Type = (Condition.ValueType, Left.ValueType, Right.ValueType) switch
        {
            // If condition is a vector, we need to use this vector size instead
            (VectorType c, _, _) => new VectorType(scalarType, c.Size),
            (ScalarType c, ScalarType, ScalarType) => scalarType,
            (ScalarType c, VectorType v1, ScalarType s2) => v1.WithElementType(scalarType),
            (ScalarType c, ScalarType s1, VectorType v2) => v2.WithElementType(scalarType),
            (ScalarType c, VectorType v1, VectorType v2) => new VectorType(scalarType, Math.Min(v1.Size, v2.Size)),
        };
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var conditionValue = Condition.CompileAsValue(table, compiler);

        // TODO: Review choice between if/else like branch (OpBranchConditional) which evaluate only one side, or select (OpSelect) which evaluate both side but can work per component but is limited to specific types
        //       It seems HLSL 2021 changed the behavior to align it with C-style short-circuiting.
        // For now, we use OpSelect only with per-component, otherwise we use if/else branching
        bool isBranching = Condition.ValueType is ScalarType;

        if (isBranching)
        {
            var resultVariable = context.Bound++;
            builder.AddFunctionVariable(context.GetOrRegister(new PointerType(Type, Specification.StorageClass.Function)), resultVariable);

            var blockMergeId = context.Bound++;
            var blockTrueId = context.Bound++;
            var blockFalseId = context.Bound++;

            // OpSelectionMerge and OpBranchConditional (will be filled later)
            builder.Insert(new OpSelectionMerge(blockMergeId, SelectionControlMask.None));
            builder.Insert(new OpBranchConditional(conditionValue.Id, blockTrueId, blockFalseId, []));

            // Block when choosing left value
            builder.CreateBlock(context, blockTrueId, $"ternary_true");
            var leftResult = Left.CompileAsValue(table, compiler);
            leftResult = builder.Convert(context, leftResult, Type);
            builder.Insert(new OpStore(resultVariable, leftResult.Id, null, []));
            builder.Insert(new OpBranch(blockMergeId));

            // Block when choosing right value
            builder.CreateBlock(context, blockFalseId, $"ternary_false");
            var rightResult = Right.CompileAsValue(table, compiler);
            rightResult = builder.Convert(context, rightResult, Type);
            builder.Insert(new OpStore(resultVariable, rightResult.Id, null, []));
            builder.Insert(new OpBranch(blockMergeId));

            builder.CreateBlock(context, blockMergeId, "ternary_merge");
            var result = builder.Insert(new OpLoad(context.GetOrRegister(Type), context.Bound++, resultVariable, null, []));
            return new(result.ResultId, result.ResultType);
        }
        else
        {
            if (Type is VectorType v && Condition.ValueType is ScalarType conditionScalar)
            {
                conditionValue = builder.Convert(context, conditionValue, new VectorType(conditionScalar, v.Size));
            }

            var leftResult = Left.CompileAsValue(table, compiler);
            leftResult = builder.Convert(context, leftResult, Type);
            var rightResult = Right.CompileAsValue(table, compiler);
            rightResult = builder.Convert(context, rightResult, Type);

            var result = builder.Insert(new OpSelect(context.GetOrRegister(Type), context.Bound++, conditionValue.Id, leftResult.Id, rightResult.Id));
            return new(result.ResultId, result.ResultType);
        }
    }

    public override string ToString()
    {
        return $"({Condition} ? {Left} : {Right})";
    }
}