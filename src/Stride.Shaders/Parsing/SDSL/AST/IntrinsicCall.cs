using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;
using System.Collections.Frozen;
using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.SDSL;

public class IntrinsicCallHelper
{
    private static IntrinsicTemplateExpander? TemplateExpander { get; set; }
    private static Dictionary<SymbolType, IntrinsicTemplateExpander> ClassTemplateExpanders = new();
    
    public static bool TryResolveIntrinsic(SymbolTable table, SymbolType? thisType, string name, SymbolType[] argumentValueTypes, out (IIntrinsicCompiler Compiler, string Namespace, IntrinsicTemplateExpander.IntrinsicOverload Overload) resolvedIntrinsic)
    {
        resolvedIntrinsic = default;

        static IntrinsicTemplateExpander GetOrCreateExpander(SymbolType type, string @namespace, FrozenDictionary<string, IntrinsicDefinition[]> intrinsicsDefinitions)
        {
            if (!ClassTemplateExpanders.TryGetValue(type, out var value))
                ClassTemplateExpanders.Add(type, value = new(type, @namespace, intrinsicsDefinitions));
            return value;
        }

        (var templateExpander, var intrinsicCompiler) = thisType switch
        {
            null => (TemplateExpander ??= new(null, nameof(IntrinsicsDefinitions.Intrinsics), IntrinsicsDefinitions.Intrinsics), (IIntrinsicCompiler)IntrinsicImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim1D, Sampled: not 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture1DMethods), IntrinsicsDefinitions.Texture1DMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim1D, Sampled: not 2, Arrayed: true, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture1DArrayMethods), IntrinsicsDefinitions.Texture1DArrayMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim1D, Sampled: 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture1DMethods), IntrinsicsDefinitions.RWTexture1DMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim1D, Sampled: 2, Arrayed: true, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture1DArrayMethods), IntrinsicsDefinitions.RWTexture1DArrayMethods), TextureMethodsImplementations.Instance),

            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: not 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture2DMethods), IntrinsicsDefinitions.Texture2DMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: not 2, Arrayed: true, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture2DArrayMethods), IntrinsicsDefinitions.Texture2DArrayMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: not 2, Arrayed: false, Multisampled: true } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture2DMSMethods), IntrinsicsDefinitions.Texture2DMSMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: not 2, Arrayed: true, Multisampled: true } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture2DArrayMSMethods), IntrinsicsDefinitions.Texture2DArrayMSMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture2DMethods), IntrinsicsDefinitions.RWTexture2DMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: 2, Arrayed: true, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture2DArrayMethods), IntrinsicsDefinitions.RWTexture2DArrayMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: 2, Arrayed: false, Multisampled: true } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture2DMSMethods), IntrinsicsDefinitions.RWTexture2DMSMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim2D, Sampled: 2, Arrayed: true, Multisampled: true } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture2DMSArrayMethods), IntrinsicsDefinitions.RWTexture2DMSArrayMethods), TextureMethodsImplementations.Instance),

            TextureType { Dimension: Specification.Dim.Dim3D, Sampled: not 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.Texture3DMethods), IntrinsicsDefinitions.Texture3DMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Dim3D, Sampled: 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWTexture3DMethods), IntrinsicsDefinitions.RWTexture3DMethods), TextureMethodsImplementations.Instance),

            TextureType { Dimension: Specification.Dim.Cube, Sampled: not 2, Arrayed: false, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.TextureCUBEMethods), IntrinsicsDefinitions.TextureCUBEMethods), TextureMethodsImplementations.Instance),
            TextureType { Dimension: Specification.Dim.Cube, Sampled: not 2, Arrayed: true, Multisampled: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.TextureCUBEArrayMethods), IntrinsicsDefinitions.TextureCUBEArrayMethods), TextureMethodsImplementations.Instance),

            BufferType { WriteAllowed: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.BufferMethods), IntrinsicsDefinitions.BufferMethods), BufferMethodsImplementations.Instance),
            BufferType { WriteAllowed: true } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWBufferMethods), IntrinsicsDefinitions.RWBufferMethods), BufferMethodsImplementations.Instance),
            
            StructuredBufferType { WriteAllowed: false } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.StructuredBufferMethods), IntrinsicsDefinitions.StructuredBufferMethods), null),
            StructuredBufferType { WriteAllowed: true } => (GetOrCreateExpander(thisType, nameof(IntrinsicsDefinitions.RWStructuredBufferMethods), IntrinsicsDefinitions.RWStructuredBufferMethods), null),
        };
        
        if (!templateExpander.TryGetOrGenerateIntrinsicsDefinition(name, out var overloads))
        {
            return false;
        }

        // Figure out the best overload
        IntrinsicTemplateExpander.IntrinsicOverload bestOverload = default;
        var bestOverloadScore = int.MaxValue;
        foreach (var overload in overloads)
        {
            var overloadScore = MethodCall.OverloadScore(overload.Type, 0, argumentValueTypes);
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

        resolvedIntrinsic = (intrinsicCompiler, templateExpander.Namespace, bestOverload);
        return true;
    }

    public static SpirvValue CompileIntrinsic(SymbolTable table, CompilerUnit compiler, IIntrinsicCompiler intrinsicCompiler, string @namespace, string name, IntrinsicTemplateExpander.IntrinsicOverload bestOverload, SpirvValue? thisValue, Span<int> compiledParams)
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
                
                results[col] = intrinsicCompiler.CompileIntrinsic(table, compiler, @namespace, name, innerFunctionType, thisValue, compiledParams).Id;
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
            result = intrinsicCompiler.CompileIntrinsic(table, compiler, @namespace, name, functionType, thisValue, compiledParams);
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
