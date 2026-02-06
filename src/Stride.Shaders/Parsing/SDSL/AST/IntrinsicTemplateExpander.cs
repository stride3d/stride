using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using SymbolType = Stride.Shaders.Core.SymbolType;

namespace Stride.Shaders.Parsing.SDSL;

/// <summary>
/// Helps expand intrinsics from <see cref="IntrinsicDefinition"/> to multiple <see cref="FunctionType"/>. 
/// </summary>
public class IntrinsicTemplateExpander(SymbolType? thisType, string @namespace, FrozenDictionary<string, IntrinsicDefinition[]> intrinsicsDefinitions)
{
    public string Namespace { get; } = @namespace;
    
    record SizePermutationGenerator(string? Name, List<int> Sizes, List<(int SourceArgument, int TemplateIndex)> Locations)
    {
        public IEnumerable<SizePermutation> Generate()
        {
            foreach (var size in Sizes)
                yield return new(size, this);
        }

        public override string ToString() => $"SizePermutationGenerator(Name={Name}, Sizes={string.Join(", ", Sizes)}, Locations={string.Join(", ", Locations)})";
    }

    record BaseTypePermutationGenerator(SymbolType[] Types, int SourceArgument)
    {
        public IEnumerable<BaseTypePermutation> Generate()
        {
            foreach (var type in Types)
                yield return new(type, this);
        }

        public override string ToString() => $"BaseTypePermutationGenerator(Types={string.Join(", ", Types)}, SourceArgument={SourceArgument})";
    }

    record SizePermutation(int Size, SizePermutationGenerator Generator);
    record BaseTypePermutation(SymbolType Type, BaseTypePermutationGenerator Generator);

    record struct SizeValue(int Value, SizePermutationGenerator Generator);
    
    public record struct IntrinsicOverload(FunctionType Type, List<(int SourceArgument, int TemplateIndex)>? AutoMatrixLoopLocations, int AutoMatrixLoopSize);
    Dictionary<string, List<IntrinsicOverload>> intrinsicDefinitionsCache = new();

    record struct ParameterTypeInfo(SymbolType BaseType, SizeValue Size1, SizeValue Size2);

    public bool TryGetOrGenerateIntrinsicsDefinition(string name, [MaybeNullWhen(false)] out List<IntrinsicOverload> result)
    {
        lock (intrinsicDefinitionsCache)
        {
            if (intrinsicDefinitionsCache.TryGetValue(name, out result))
                return true;

            if (!intrinsicsDefinitions.TryGetValue(name, out var intrinsicDefinitions))
            {
                result = null;
                return false;
            }
            
            result = new();
            foreach (var intrinsicDefinition in intrinsicDefinitions)
            {
                List<BaseTypePermutationGenerator> baseTypePermutationGenerators = new();
                List<SizePermutationGenerator> sizePermutationGenerators = new();

                void AddVectorSizePermutation(int argument, int templateIndex, string name)
                {
                    SizePermutationGenerator permutation;
                    
                    // name can be either a value (1,2,3,4,any) or a name (when multiple slots adjusted with same permutation, in which case value is [1,2,3,4]).
                    switch (name)
                    {
                        case "any" or "1" or "2" or "3" or "4":
                            permutation = new SizePermutationGenerator(null, name switch
                            {
                                "any" => [1,2,3,4],
                                "1" => [1],
                                "2" => [2],
                                "3" => [3],
                                "4" => [4],
                            }, new());
                            sizePermutationGenerators.Add(permutation);
                            break;
                        default:
                            // use name as key (and find existing one if already declared)
                            permutation = sizePermutationGenerators.FirstOrDefault(x => x.Name == name);
                            if (permutation == null)
                            {
                                permutation = new SizePermutationGenerator(name, [1,2,3,4], new());
                                sizePermutationGenerators.Add(permutation);
                            }
                            break;
                    }

                    permutation.Locations.Add((argument, templateIndex));
                }
                void AddBaseTypePermutation(int argument, SymbolType[] types)
                {
                    baseTypePermutationGenerators.Add(new BaseTypePermutationGenerator(types, argument));
                }

                // Step 1: Find unconstrained patterns
                for (var index = 0; index < intrinsicDefinition.Parameters.Length + 1; index++)
                {
                    var parameterType = index > 0 ? intrinsicDefinition.Parameters[index - 1].Type : intrinsicDefinition.Return;
                    
                    // Find which part can permutate freely
                    var isLayoutFree = parameterType.Match == null || parameterType.Match.Value.Layout == index;
                    var isBaseTypeFree = parameterType.Match == null || parameterType.Match.Value.BaseType == index;
                    //var isVectorSizeFree = parameterType.Match == null || parameterType.Match.Value.Size == 0;

                    if (parameterType.VectorSize is {} vectorSize)
                    {
                        // Note: even if size is set using match (isLayoutFree is true), it can still be overriden (value is anything else than "any") so check for it
                        if (isLayoutFree || vectorSize.X != "any")
                        {
                            AddVectorSizePermutation(index, 0, vectorSize.X);
                            if (vectorSize.Y is { } vectorSizeY)
                            {
                                AddVectorSizePermutation(index, 1, vectorSizeY);
                            }
                        }
                    }
                    
                    if (isBaseTypeFree)
                    {
                        SymbolType[] baseTypes = parameterType.BaseType switch
                        {
                            BaseType.Bool => [ ScalarType.Boolean ],
                            BaseType.Int => [ ScalarType.Int ],
                            BaseType.Int32Only =>  [ ScalarType.Int ],
                            BaseType.Int16 => throw new NotImplementedException(),
                            BaseType.Int64 =>  [ ScalarType.Int64 ],
                            BaseType.SInt16Or32 => [ ScalarType.Int ],
                            BaseType.AnyInt => [ ScalarType.Int, ScalarType.UInt, ScalarType.Int64, ScalarType.UInt64 ],
                            BaseType.AnyInt16Or32 => [ ScalarType.Int, ScalarType.UInt ],
                            BaseType.AnyInt32 => [ ScalarType.Int, ScalarType.UInt ],
                            BaseType.AnyInt64 => [ ScalarType.Int64, ScalarType.UInt64 ],
                            BaseType.Int64Only => [ ScalarType.Int64 ],
                            BaseType.Uint => [ ScalarType.UInt ],
                            BaseType.Uint16 => throw new NotImplementedException(),
                            BaseType.U64 => [ ScalarType.UInt64 ],
                            BaseType.Float => [ ScalarType.Float ],
                            BaseType.Float16 => throw new NotImplementedException(),
                            BaseType.AnyFloat => [ ScalarType.Float, ScalarType.Double ],
                            BaseType.FloatLike => [ ScalarType.Float ],
                            BaseType.Float32Only => [ ScalarType.Float ],
                            BaseType.DoubleOnly => [ ScalarType.Double ],
                            BaseType.Sampler1d => throw new NotImplementedException(), 
                            BaseType.Sampler2d => throw new NotImplementedException(),
                            BaseType.Sampler3d => throw new NotImplementedException(),
                            BaseType.SamplerCube => throw new NotImplementedException(),
                            BaseType.SamplerCmp => [ new SamplerType() ],
                            BaseType.Sampler => [ new SamplerType() ],
                            BaseType.AnySampler => [ new SamplerType() ],
                            BaseType.Wave => throw new NotImplementedException(),
                            BaseType.Void => [ ScalarType.Void ],
                            BaseType.Texture2D => throw new NotImplementedException(),
                            BaseType.UIntOnly => [ ScalarType.UInt, ScalarType.UInt64 ],
                            BaseType.Numeric => [ ScalarType.Float, ScalarType.Double, ScalarType.Int, ScalarType.UInt, ScalarType.Int64, ScalarType.UInt64 ],
                            BaseType.Numeric16Only => throw new NotImplementedException(),
                            BaseType.Numeric32Only => [ ScalarType.Float, ScalarType.Double, ScalarType.Int, ScalarType.UInt ],
                            BaseType.Any => [ ScalarType.Float, ScalarType.Double, ScalarType.Int, ScalarType.UInt, ScalarType.Int64, ScalarType.UInt64, ScalarType.Boolean ],
                            BaseType.Match => throw new InvalidOperationException(),
                            BaseType.ByteAddressBuffer => throw new NotImplementedException(),
                            BaseType.RWByteAddressBuffer => throw new NotImplementedException(),
                            BaseType.VkBufferPointer => throw new NotImplementedException(),
                            BaseType.Other => throw new NotImplementedException(),
                            BaseType.Texture2DArray => throw new NotImplementedException(),
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        AddBaseTypePermutation(index, baseTypes);
                    }
                }
                
                // Step 2: generate permutations for base types
                var baseTypeSequences = new List<List<BaseTypePermutation>>();
                foreach (var baseTypePermutationGenerator in baseTypePermutationGenerators)
                    baseTypeSequences.Add(new(baseTypePermutationGenerator.Generate()));
                var baseTypePermutations = CartesianProduct.Generate(baseTypeSequences);

                // Step 3: generate permutations for vector/matrix sizes
                var sizeSequences = new List<List<SizePermutation>>();
                foreach (var sizePermutationGenerator in sizePermutationGenerators)
                    sizeSequences.Add(new(sizePermutationGenerator.Generate()));
                var sizePermutations = CartesianProduct.Generate(sizeSequences);
                
                // Step 4: generate signature using permutations
                ParameterTypeInfo[] parameterTypeHelper = new ParameterTypeInfo[intrinsicDefinition.Parameters.Length + 1];
                SymbolType[] parameterTypes = new SymbolType[intrinsicDefinition.Parameters.Length + 1];
                foreach (var baseTypePermutationList in baseTypePermutations)
                {
                    Array.Clear(parameterTypeHelper);
                    // Use base type permutations to fill initial types
                    foreach (var baseTypePermutation in baseTypePermutationList)
                        parameterTypeHelper[baseTypePermutation.Generator.SourceArgument].BaseType = baseTypePermutation.Type;
                    
                    // Set other parameters (which might use initial types)
                    // Only Match type should be left
                    for (var index = 0; index < intrinsicDefinition.Parameters.Length + 1; index++)
                    {
                        var parameterType = index > 0 ? intrinsicDefinition.Parameters[index - 1].Type : intrinsicDefinition.Return;
                        // Make sure this parameter is not set yet by something else
                        if (parameterTypeHelper[index].BaseType == null)
                        {
                            // Note: we also check match index doesn't point back to us
                            if (parameterType.Match == null || parameterType.Match.Value.BaseType == index)
                                throw new InvalidOperationException($"Intrinsic {name}: Can't resolve parameter {index} of type {parameterType}");

                            parameterTypeHelper[index].BaseType = parameterTypeHelper[parameterType.Match.Value.BaseType].BaseType;
                        } 
                    }
                    
                    // Now, iterate on size permutations
                    bool firstIteration = true;
                    foreach (var sizePermutationList in sizePermutations)
                    {
                        // Reset Sizes
                        for (var index = 0; index < intrinsicDefinition.Parameters.Length + 1; index++)
                        {
                            parameterTypeHelper[index].Size1 = default;
                            parameterTypeHelper[index].Size2 = default;
                        }
                        
                        foreach (var sizePermutation in sizePermutationList)
                        {
                            foreach (var location in sizePermutation.Generator.Locations)
                            {
                                switch (location.TemplateIndex)
                                {
                                    case 0:
                                        parameterTypeHelper[location.SourceArgument].Size1 = new(sizePermutation.Size, sizePermutation.Generator);
                                        break;
                                    case 1:
                                        parameterTypeHelper[location.SourceArgument].Size2 = new(sizePermutation.Size, sizePermutation.Generator);
                                        break;
                                }
                            }
                        }

                        ParameterTypeInfo GetParameterInfo(int index)
                        {
                            if (index == -1)
                            {
                                return thisType switch
                                {
                                    null => throw new ArgumentNullException(nameof(thisType)),
                                    TextureType t => new(t.ReturnType, new(4, null), default),
                                    BufferType b => new(b.BaseType, new(4, null), default),
                                };
                            }
                            return parameterTypeHelper[index];
                        }

                        // Use match() to fill size info
                        for (var index = 0; index < intrinsicDefinition.Parameters.Length + 1; index++)
                        {
                            var parameterType = index > 0 ? intrinsicDefinition.Parameters[index - 1].Type : intrinsicDefinition.Return;
                            // Make sure this parameter is not set yet by something else
                            if (parameterTypeHelper[index].Size1.Value == 0)
                            {
                                if (parameterType.Match != null && parameterType.Match.Value.Layout != index)
                                {
                                    var paramInfo = GetParameterInfo(parameterType.Match.Value.Layout);
                                    if (parameterTypeHelper[index].BaseType == ScalarType.Void)
                                        parameterTypeHelper[index].BaseType = paramInfo.BaseType;
                                    parameterTypeHelper[index].Size1 = paramInfo.Size1;
                                    parameterTypeHelper[index].Size2 = paramInfo.Size2;

                                    // Also register locations (to easily analyze matrix loops later) 
                                    if (firstIteration)
                                    {
                                        parameterTypeHelper[index].Size1.Generator?.Locations.Add(new(index, 0));
                                        parameterTypeHelper[index].Size2.Generator?.Locations.Add(new(index, 1));
                                    }
                                }
                            }
                        }

                        firstIteration = false;

                        List<int>? autoMatrixLoopArguments = null;
                        SizePermutationGenerator? autoMatrixLoop = null;
                        FunctionType? autoMatrixLoopType = null;
                        int autoMatrixLoopSize = 0;
                        
                        // Generate real types using sizes
                        for (var index = 0; index < intrinsicDefinition.Parameters.Length + 1; index++)
                        {
                            ref var resolvedBaseType = ref parameterTypeHelper[index];
                            
                            if (resolvedBaseType.Size1.Value > 1 && resolvedBaseType.Size2.Value > 1)
                            {
                                if (resolvedBaseType.Size1.Generator.Name == null)
                                {
                                    // If matrix types are generated from a size generator (without a name so that there is no specific row/column pattern like in mul()),
                                    // we can automatically convert a call to multiple calls on each inner vector.
                                    // So we try to remember this info here
                                    if (autoMatrixLoop != null && autoMatrixLoop != resolvedBaseType.Size1.Generator)
                                        throw new InvalidOperationException("Multiple matrix with different generators");
                                    autoMatrixLoop = resolvedBaseType.Size1.Generator;
                                    autoMatrixLoopSize = resolvedBaseType.Size1.Value;
                                }
                                parameterTypes[index] = new MatrixType((ScalarType)resolvedBaseType.BaseType, resolvedBaseType.Size2.Value, resolvedBaseType.Size1.Value);
                            }
                            // Note: since in HLSL float4x1 and float1x4 maps to SPIR-V float4, we will have duplicates (but not a big deal)
                            else if (resolvedBaseType.Size1.Value > 1 || resolvedBaseType.Size2.Value > 1)
                            {
                                parameterTypes[index] = new VectorType((ScalarType)resolvedBaseType.BaseType, Math.Max(resolvedBaseType.Size1.Value, resolvedBaseType.Size2.Value));
                            }
                            else
                            {
                                parameterTypes[index] = resolvedBaseType.BaseType;
                            }
                        }
                        
                        // Note: we remove auto matrix loop if result type is not either void or matrix of the desired size
                        if (autoMatrixLoop != null)
                        {
                            if (parameterTypes[0] != ScalarType.Void && (parameterTypes[0] is not MatrixType || parameterTypeHelper[0].Size1.Generator != autoMatrixLoop))
                                autoMatrixLoop = null;
                        }

                        var functionParameters = new List<FunctionParameter>();
                        for (int i = 0; i < intrinsicDefinition.Parameters.Length; ++i)
                        {
                            functionParameters.Add(new(parameterTypes[i + 1], intrinsicDefinition.Parameters[i].Qualifier switch
                            {
                                Qualifier.In => ParameterModifiers.In,
                                Qualifier.Out => ParameterModifiers.Out,
                                Qualifier.InOut or Qualifier.Ref => ParameterModifiers.InOut,
                                null => ParameterModifiers.None,
                            }));
                        }
                        var functionType = new FunctionType(parameterTypes[0], functionParameters);
                        
                        result.Add(new(functionType, autoMatrixLoop?.Locations, autoMatrixLoopSize));
                    }
                }
            }
            
            intrinsicDefinitionsCache.Add(name, result);
            return true;
        }
    }
    
    /// <summary>
    /// Helper class to generate all permutations using cartesian product.
    /// </summary>
    class CartesianProduct
    {
        public static List<List<T>> Generate<T>(List<List<T>> sequences)
        {
            var result = new List<List<T>>();
            if (sequences == null || sequences.Count == 0)
            {
                return result;
            }

            CartesianRecurse(result, new List<T>(), sequences, 0);
            return result;
        }

        private static void CartesianRecurse<T>(List<List<T>> accumulator, List<T> currentCombination, List<List<T>> sequences, int sequenceIndex)
        {
            // Base case: If we have processed all sequences, add the current combination to the result
            if (sequenceIndex == sequences.Count)
            {
                accumulator.Add(new List<T>(currentCombination));
                return;
            }

            // Recursive step: Iterate through the current sequence
            foreach (T item in sequences[sequenceIndex])
            {
                currentCombination.Add(item);
                // Recurse to the next sequence
                CartesianRecurse(accumulator, currentCombination, sequences, sequenceIndex + 1);
                // Backtrack: Remove the last added item to explore the next item in the current sequence
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }
    }
}