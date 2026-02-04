using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;

namespace Stride.Shaders.Parsing.SDSL;

public class IntrinsicCall(Identifier name, ShaderExpressionList arguments, TextLocation info) : MethodCall(name, arguments, info)
{
    private static IntrinsicTemplateExpander TemplateExpander { get; } = new();
    private IntrinsicTemplateExpander.IntrinsicOverload BestOverload { get; set; }
    
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        // Process arguments
        ProcessParameterSymbols(table);

        var overloads = TemplateExpander.GetOrGenerateIntrinsicsDefinition(Name.Name);
        
        // Figure out the best overload
        BestOverload = default;
        var bestOverloadScore = int.MaxValue;
        foreach (var overload in overloads)
        {
            var overloadScore = OverloadScore(overload.Type, 0, Arguments);
            if (overloadScore < bestOverloadScore)
            {
                // Better overload
                BestOverload = overload;
                bestOverloadScore = overloadScore;
                // We won't get better than that (perfect match), stop there
                if (overloadScore == 0)
                    break;
            }
        }
        
        if (BestOverload.Type == null)
            throw new InvalidOperationException($"No overload found for intrinsic {Name} with arguments {Arguments}");
        
        // Now we know the return type
        Type = BestOverload.Type.ReturnType;
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var functionType = BestOverload.Type;
        
        Span<int> compiledParams = stackalloc int[functionType.ParameterTypes.Count];
        
        ProcessInputArguments(table, compiler, functionType, compiledParams);

        // Check if we can automatically handle matrix (SPIR-V doesn't but HLSL does allow matrix on most types)
        SpirvValue result;
        if (BestOverload.AutoMatrixLoopLocations != null)
        {
            var (builder, context) = compiler;

            var innerFunctionType = new FunctionType(functionType.ReturnType, functionType.ParameterTypes.ToList());
            
            // Extract rows
            bool isReturnUsingLoop = false;
            Span<int> vectorValues = stackalloc int[BestOverload.AutoMatrixLoopLocations.Count * BestOverload.AutoMatrixLoopSize];
            for (var index = 0; index < BestOverload.AutoMatrixLoopLocations.Count; index++)
            {
                var location = BestOverload.AutoMatrixLoopLocations[index];

                if (location.TemplateIndex != 0)
                    throw new InvalidOperationException("Matrix loop should only be generated for HLSL row parameter");
                
                // Skip return type for now
                if (location.SourceArgument == 0)
                {
                    var returnType = (MatrixType)functionType.ReturnType;
                    innerFunctionType = innerFunctionType with
                    {
                        ReturnType = new VectorType(returnType.BaseType, returnType.Rows),
                    };
                    isReturnUsingLoop = true;
                    continue;
                }

                var parameterType = (MatrixType)functionType.ParameterTypes[location.SourceArgument - 1].Type;
                var vectorType = new VectorType(parameterType.BaseType, parameterType.Rows);
                for (int col = 0; col < BestOverload.AutoMatrixLoopSize; col++)
                {
                    vectorValues[index * BestOverload.AutoMatrixLoopSize + col] = builder.Insert(new OpCompositeExtract(context.GetOrRegister(vectorType), context.Bound++, compiledParams[location.SourceArgument - 1], [col])).ResultId;
                }
                
                innerFunctionType.ParameterTypes[location.SourceArgument - 1] = innerFunctionType.ParameterTypes[location.SourceArgument - 1] with { Type = vectorType }; 
            }
            
            // Call core function
            Span<int> results = stackalloc int[BestOverload.AutoMatrixLoopSize];
            for (int col = 0; col < BestOverload.AutoMatrixLoopSize; col++)
            {
                for (var index = 0; index < BestOverload.AutoMatrixLoopLocations.Count; index++)
                {
                    var location = BestOverload.AutoMatrixLoopLocations[index];
                    if (location.SourceArgument == 0)
                        continue;
                    compiledParams[location.SourceArgument - 1] = vectorValues[index * BestOverload.AutoMatrixLoopSize + col];
                }
                
                results[col] = IntrinsicImplementations.Instance.CompileIntrinsic(table, compiler, Name.Name, innerFunctionType, compiledParams).Id;
            }
            
            // Rebuild return value
            if (isReturnUsingLoop)
            {
                if (Type is not MatrixType)
                    throw new InvalidOperationException("Return type should be a matrix");
                
                result = new(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(Type), context.Bound++, [..results])));
            }
            else
            {
                result = new();
            }
        }
        else
        {
            // No auto matrix loop
            result = IntrinsicImplementations.Instance.CompileIntrinsic(table, compiler, Name.Name, functionType, compiledParams);
        }
        
        ProcessOutputArguments(table, compiler, functionType, compiledParams);

        return result;
    }
}

public enum InterlockedOp
{
    Add,
    And,
    Or,
    Xor,
    Max,
    Min,
    Exchange,
    CompareExchange,
    CompareStore,
}
