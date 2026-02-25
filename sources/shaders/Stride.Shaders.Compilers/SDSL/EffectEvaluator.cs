using CommunityToolkit.HighPerformance;
using Stride.Rendering;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL
{
    // Note: We currently use EffectCodeWriter to generate C# code instead.
    //       Kept around if we later switch to a full SPIR-V approach.
    [Obsolete("Use C# EffectCodeWriter (and ShaderMixinManager) for now. Kept for future SPIR-V switch.")]
    internal class EffectEvaluator(IExternalShaderLoader shaderLoader)
    {
        private Stack<ShaderMixinSource> mixinSources = new();

        public ShaderSource EvaluateEffects(ShaderSource source, IDictionary<string, ParameterKey>? parameters = null)
        {
            // Note: we currently use EffectCodeWriter to generate C# code instead
            throw new NotImplementedException();
            // For our tests the ShaderSource is a ShaderClassSource, just a name of a shader to load

            switch (source)
            {
                case ShaderClassSource classSource:
                    var macros = mixinSources.Count > 0 ? mixinSources.Peek().Macros : [];
                    var shaderBuffers = SpirvBuilder.GetOrLoadShader(shaderLoader, classSource.ClassName, classSource.GenericArguments, macros.AsSpan());
                    throw new NotImplementedException();
                    return shaderBuffers.Buffer[0].Op switch
                    {
                        Op.OpEffectSDFX => EffectInterpreter(shaderBuffers, parameters),
                        _ => classSource
                    };
                case ShaderMixinSource mixinSource:
                    {
                        var result = new ShaderMixinSource();
                        foreach (var macro in mixinSource.Macros)
                            result.Macros.Add(macro);

                        if (mixinSources.Count > 0)
                            PropagateMacrosFromParent(mixinSources.Peek(), result);

                        mixinSources.Push(result);
                        try
                        {
                            foreach (var mixin in mixinSource.Mixins)
                            {
                                var evaluatedMixin = EvaluateEffects(mixin);
                                Merge(result, evaluatedMixin);
                            }

                            foreach (var composition in mixinSource.Compositions)
                            {
                                var evaluatedMixin = EvaluateEffects(composition.Value);
                                MergeComposition(result, composition.Key, evaluatedMixin);
                            }
                        }
                        finally
                        {
                            mixinSources.Pop();
                        }
                        return result;
                    }
                case ShaderArraySource arraySource:
                    {
                        var result = new ShaderArraySource();
                        foreach (var mixin in arraySource.Values)
                        {
                            var evaluatedMixin = EvaluateEffects(mixin);
                            result.Add(evaluatedMixin);
                        }
                        return result;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private ShaderMixinSource EffectInterpreter(ShaderBuffers shaderBuffers, IDictionary<string, ParameterKey>? parameters)
        {
            var mixinTree = new ShaderMixinSource();
            int i = 0;
            var count = shaderBuffers.Buffer.Count;
            while (i < count)
            {
                var instruction = shaderBuffers.Buffer[i];

                if (instruction.Op == Op.OpMixinSDFX && (OpMixinSDFX)instruction is { } mixinInstruction)
                {
                    // Note: we currently use EffectCodeWriter to generate C# code instead
                    throw new NotImplementedException();
                    string DecodeString(int id) => throw new NotImplementedException();
                    var instSource = new ShaderClassSource(DecodeString(mixinInstruction.Value), mixinInstruction.Values);
                    var evaluatedSource = EvaluateEffects(instSource);

                    switch (mixinInstruction.Kind)
                    {
                        case MixinKindSDFX.Default:
                            Merge(mixinTree, evaluatedSource);
                            break;
                        case MixinKindSDFX.ComposeSet:
                            MergeComposition(mixinTree, DecodeString(mixinInstruction.Target), evaluatedSource);
                            break;
                        case MixinKindSDFX.ComposeAdd:
                            MergeCompositionArrayItem(mixinTree, DecodeString(mixinInstruction.Target), evaluatedSource);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                i += 1;
            }

            return mixinTree;
        }

        private void PropagateMacrosFromParent(ShaderMixinSource parent, ShaderMixinSource child)
        {
            var existingMacros = new HashSet<string>();
            foreach (var macro in child.Macros)
            {
                existingMacros.Add(macro.Name);
            }
            foreach (var macro in parent.Macros)
            {
                if (!existingMacros.Contains(macro.Name))
                    child.AddMacro(macro.Name, macro.Definition);
            }
        }

        public void Merge(ShaderMixinSource mixinTree, ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource classSource:
                    mixinTree.Mixins.Add(classSource);
                    break;
                case ShaderMixinSource mixinSource:
                    mixinTree.Macros.AddRange(mixinSource.Macros);
                    mixinTree.Mixins.AddRange(mixinSource.Mixins);

                    foreach (var composition in mixinSource.Compositions)
                    {
                        MergeComposition(mixinTree, composition.Key, composition.Value);
                    }

                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public void MergeComposition(ShaderMixinSource mixinTree, string compositionName, ShaderSource compositionToAdd)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions[compositionName] = composition = compositionToAdd is ShaderArraySource ? new ShaderArraySource() : new ShaderMixinSource();

            if (compositionToAdd is ShaderArraySource compositionArrayToAdd)
            {
                var compositionArray = (ShaderArraySource)composition;
                compositionArray.Values.AddRange(compositionArrayToAdd);
            }
            else
            {
                Merge((ShaderMixinSource)composition, compositionToAdd);
            }
        }

        public void MergeCompositionArrayItem(ShaderMixinSource mixinTree, string compositionName, ShaderSource evaluatedSource)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions.Add(compositionName, composition = new ShaderArraySource());

            var arraySource = (ShaderArraySource)composition;
            arraySource.Add(evaluatedSource);
        }

        static object[] GetGenericsArguments(SpirvContext context, ReadOnlySpan<int> genericIds)
        {
            var genericArguments = new object[genericIds.Length];
            for (int i = 0; i < genericArguments.Length; i++)
            {
                genericArguments[i] = context.GetConstantValue(genericIds[i]);
            }
            return genericArguments;
        }
    }
}
