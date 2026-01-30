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
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL
{
    internal class EffectEvaluator(IExternalShaderLoader ShaderLoader)
    {
        private Stack<ShaderMixinSource> mixinSources = new();

        public ShaderSource EvaluateEffects(ShaderSource source, IDictionary<string, ParameterKey>? parameters = null)
        {
            // For our tests the ShaderSource is a ShaderClassSource, just a name of a shader to load

            switch (source)
            {
                case ShaderClassSource classSource:
                    var macros = mixinSources.Count > 0 ? mixinSources.Peek().Macros : [];
                    var shaderBuffers = SpirvBuilder.GetOrLoadShader(ShaderLoader, classSource.ClassName, classSource.GenericArguments, macros.AsSpan());
                    return shaderBuffers.Buffer[0].Op switch
                    {
                        Op.OpSDSLEffect => EffectInterpreter(shaderBuffers, parameters),
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

                // If we reach a conditional instruction we need to evaluate the conditions after it.
                // If it's false we need to check if there's another conditional instruction after it (ParamsTrue / Else)
                // Once we reach the OpSDSLConditionalEnd 
                if (instruction.Op is Op.OpSDSLConditionalStart)
                {
                    i += 1;
                    bool conditionMet = false;
                    while(!conditionMet)
                    {
                        if (instruction.Op is Op.OpSDSLParamsTrue && (OpSDSLParamsTrue)instruction is { } condition)
                        {
                            if (parameters?.TryGetValue(condition.ParamsName, out var bparam) ?? false)
                            {
                                // TODO: Where are the values ?
                                if (bparam is ParameterKey<bool> boolParam)
                                {
                                    throw new NotImplementedException();
                                }
                                else if (bparam is ParameterKey<ShaderSource> shparam)
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        }
                        else if(instruction.Op is Op.OpSDSLElse)
                        {
                            if(!conditionMet)
                            {
                                conditionMet = true;
                                // TODO: Apply else branch
                            }
                        }
                        else if(instruction.Op is Op.OpSDSLConditionalEnd)
                        {
                            break;
                        }
                    }
                }
                else if (instruction.Op == Op.OpSDSLMixin && (OpSDSLMixin)instruction is { } mixinInstruction)
                {
                    var instSource = new ShaderClassSource(mixinInstruction.Mixin, GetGenericsArguments(shaderBuffers.Context, mixinInstruction.Values.Elements.Span));
                    var evaluatedSource = EvaluateEffects(instSource);

                    Merge(mixinTree, evaluatedSource);
                }
                else if (instruction.Op == Op.OpSDSLMixinCompose && (OpSDSLMixinCompose)instruction is { } mixinComposeInstruction)
                {
                    var instSource = new ShaderClassSource(mixinComposeInstruction.Mixin, GetGenericsArguments(shaderBuffers.Context, mixinComposeInstruction.Values.Elements.Span));
                    var evaluatedSource = EvaluateEffects(instSource);

                    MergeComposition(mixinTree, mixinComposeInstruction.Identifier, evaluatedSource);
                }
                else if (instruction.Op == Op.OpSDSLMixinComposeArray && (OpSDSLMixinComposeArray)instruction is { } mixinComposeArray)
                {
                    var instSource = new ShaderClassSource(mixinComposeArray.Mixin, GetGenericsArguments(shaderBuffers.Context, mixinComposeArray.Values.Elements.Span));
                    var evaluatedSource = EvaluateEffects(instSource);

                    MergeCompositionArrayItem(mixinTree, mixinComposeArray.Identifier, evaluatedSource);
                }
                else if (instruction.Op == Op.OpSDSLMixinChild && (OpSDSLMixinChild)instruction is { } mixinChild)
                {
                    throw new NotImplementedException();
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
                mixinTree.Compositions.Add(compositionName, composition = compositionToAdd is ShaderArraySource ? new ShaderArraySource() : new ShaderMixinSource());

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
