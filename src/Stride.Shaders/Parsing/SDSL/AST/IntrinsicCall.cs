using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;

namespace Stride.Shaders.Parsing.SDSL;

public class IntrinsicCallHelper
{
    private static IntrinsicTemplateExpander TemplateExpander { get; } = new(IntrinsicsDefinitions.Intrinsics);
    private IntrinsicTemplateExpander.IntrinsicOverload BestOverload { get; set; }
    
    public static bool TryResolveIntrinsic(SymbolTable table, string name, ShaderExpressionList arguments, out IntrinsicTemplateExpander.IntrinsicOverload bestOverload)
    {
        bestOverload = default;
        if (!TemplateExpander.TryGetOrGenerateIntrinsicsDefinition(name, out var overloads))
        {
            return false;
        }

        // Figure out the best overload
        bestOverload = default;
        var bestOverloadScore = int.MaxValue;
        foreach (var overload in overloads)
        {
            var overloadScore = MethodCall.OverloadScore(overload.Type, 0, arguments);
            if (overloadScore < bestOverloadScore)
            {
                // Better overload
                bestOverload = overload;
                bestOverloadScore = overloadScore;
                // We won't get better than that (perfect match), stop there
                if (overloadScore == 0)
                    break;
            }
        }

        if (bestOverloadScore == int.MaxValue)
            return false;

        return true;
    }

    public static SpirvValue CompileIntrinsic(SymbolTable table, CompilerUnit compiler, string name, IntrinsicTemplateExpander.IntrinsicOverload bestOverload, Span<int> compiledParams)
    {
        var functionType = bestOverload.Type;
        
        // Check if we can automatically handle matrix (SPIR-V doesn't but HLSL does allow matrix on most types)
        SpirvValue result;
        if (bestOverload.AutoMatrixLoopLocations != null)
        {
            var (builder, context) = compiler;

            var innerFunctionType = new FunctionType(functionType.ReturnType, functionType.ParameterTypes.ToList());
            
            // Extract rows
            bool isReturnUsingLoop = false;
            Span<int> vectorValues = stackalloc int[bestOverload.AutoMatrixLoopLocations.Count * bestOverload.AutoMatrixLoopSize];
            for (var index = 0; index < bestOverload.AutoMatrixLoopLocations.Count; index++)
            {
                var location = bestOverload.AutoMatrixLoopLocations[index];

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
                for (int col = 0; col < bestOverload.AutoMatrixLoopSize; col++)
                {
                    vectorValues[index * bestOverload.AutoMatrixLoopSize + col] = builder.Insert(new OpCompositeExtract(context.GetOrRegister(vectorType), context.Bound++, compiledParams[location.SourceArgument - 1], [col])).ResultId;
                }
                
                innerFunctionType.ParameterTypes[location.SourceArgument - 1] = innerFunctionType.ParameterTypes[location.SourceArgument - 1] with { Type = vectorType }; 
            }
            
            // Call core function
            Span<int> results = stackalloc int[bestOverload.AutoMatrixLoopSize];
            for (int col = 0; col < bestOverload.AutoMatrixLoopSize; col++)
            {
                for (var index = 0; index < bestOverload.AutoMatrixLoopLocations.Count; index++)
                {
                    var location = bestOverload.AutoMatrixLoopLocations[index];
                    if (location.SourceArgument == 0)
                        continue;
                    compiledParams[location.SourceArgument - 1] = vectorValues[index * bestOverload.AutoMatrixLoopSize + col];
                }
                
                results[col] = IntrinsicImplementations.Instance.CompileIntrinsic(table, compiler, name, innerFunctionType, compiledParams).Id;
            }
            
            // Rebuild return value
            if (isReturnUsingLoop)
            {
                if (functionType.ReturnType is not MatrixType)
                    throw new InvalidOperationException("Return type should be a matrix");
                
                result = new(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(functionType.ReturnType), context.Bound++, [..results])));
            }
            else
            {
                result = new();
            }
        }
        else
        {
            // No auto matrix loop
            result = IntrinsicImplementations.Instance.CompileIntrinsic(table, compiler, name, functionType, compiledParams);
        }

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
